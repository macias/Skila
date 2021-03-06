﻿using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Flow;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Threading.Tasks;
using System.Threading;
using Skila.Language.Expressions.Literals;
using System.Diagnostics;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        public static int TimeoutSeconds;

        public const string CommandLineTestProgramPath = "hello_world.out";
        public const string CommandLineTestArgument = "hello";

        private readonly bool debugMode;

        public Interpreter(bool debugMode = false)
        {
            this.debugMode = debugMode;
        }
        private static async Task<ObjectData> createChunk(ExecutionContext ctx, EntityInstance chunkRunTimeInstance, ObjectData[] elements)
        {
            return await ObjectData.CreateInstanceAsync(ctx, chunkRunTimeInstance, new Chunk(elements)).ConfigureAwait(false);
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, FunctionDefinition func)
        {
            if (func.IsDeclaration)
                throw new ArgumentException($"Selected declaration for execution {ExceptionCode.SourceInfo()}");

            ctx.Translation = TemplateTranslation.Create(func, ctx.TemplateArguments);

            // in case of the extension within the function we use first parameter as regular one
            if (ctx.ThisArgument != null && !func.IsExtension)
            {
                ctx.LocalVariables.Add(func.MetaThisParameter, ctx.ThisArgument);
                ObjectData this_value = ctx.ThisArgument.DereferencedOnce();
                bool dummy = false;
                ctx.Translation = TemplateTranslation.Translated(ctx.Translation, this_value.RunTimeTypeInstance.Translation, ref dummy);
            }

            {
                for (int i = 0; i < func.Parameters.Count; ++i)
                {
                    FunctionParameter param = func.Parameters[i];
                    ObjectData arg_data;
                    if (i == 0 && func.IsExtension && ctx.ThisArgument != null)
                        arg_data = ctx.ThisArgument;
                    else
                    {
                        if (ctx.FunctionArguments[i] == null)
                            ctx.FunctionArguments[i] = (await ExecutedAsync(param.DefaultValue, ctx).ConfigureAwait(false)).ExprValue;
                        arg_data = ctx.FunctionArguments[i];
                    }

                    if (!ctx.LocalVariables.Add(param, arg_data))
                        throw new ArgumentException($"{ExceptionCode.SourceInfo()}");
                }
            }

            if (func.IsNewConstructor())
            {
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(false);
            }
            // not all methods in plain types (like Int,Double) are native
            // so we check just a function modifier
            else if (func.Modifier.HasNative)
            {
                return await executeNativeFunctionAsync(ctx, func).ConfigureAwait(false);
            }
            else
            {
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(false);
            }
        }

        private async Task<ExecValue> executeRegularFunctionAsync(FunctionDefinition func, ExecutionContext ctx)
        {
            if (func.IsDeclaration)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ExecValue ret = await ExecutedAsync(func.UserBody, ctx).ConfigureAwait(false);
            if (ctx.Env.IsUnitType(func.ResultTypeName.Evaluation.Components))
                return ExecValue.CreateReturn(null);
            else
                return ret;
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, Block block)
        {
            return executeAsync(ctx, block.Instructions);
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, New @new)
        {
            return executeAsync(ctx, @new.Instructions);
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, Chain chain)
        {
            return executeAsync(ctx, chain.Instructions);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, IEnumerable<IExpression> instructions)
        {
            ExecValue result = ExecValue.UndefinedExpression;

            foreach (IExpression expr in instructions)
            {
                result = await ExecutedAsync(expr, ctx).ConfigureAwait(false);
                if (!result.IsExpression)
                    break;
            }

            return result;
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Loop loop)
        {
            ExecValue result = ExecValue.UndefinedExpression;

            while (true)
            {
                ctx.LocalVariables.AddLayer(loop);

                result = await iterateLoopAsync(ctx, loop).ConfigureAwait(false);
                exitScope(ctx, loop, result);

                if (!result.IsExpression
                    // if iteration was stopped due to failed condition we would have here the outcome of the condition
                    || result.ExprValue != null)
                    break;
            }

            return result;
        }

        private async Task<ExecValue> iterateLoopAsync(ExecutionContext ctx, Loop loop)
        {
            ExecValue result;

            if (loop.PreCondition != null)
            {
                result = await ExecutedAsync(loop.PreCondition, ctx).ConfigureAwait(false);
                if (!result.IsExpression)
                    return result;
                bool cond = result.ExprValue.PlainValue.Cast<bool>();
                if (!cond)
                    return result;
            }

            result = await executeAsync(ctx, loop.Body).ConfigureAwait(false);
            if (!result.IsExpression)
                return result;

            result = await executeAsync(ctx, loop.PostStep).ConfigureAwait(false);
            if (!result.IsExpression)
                return result;

            if (loop.PostCondition != null)
            {
                result = await ExecutedAsync(loop.PostCondition, ctx).ConfigureAwait(false);
                if (!result.IsExpression)
                    return result;
                bool cond = result.ExprValue.PlainValue.Cast<bool>();
                if (!cond)
                    return result;
            }

            return ExecValue.UndefinedExpression;
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, IfBranch ifBranch)
        {
            ObjectData cond_obj = null;
            if (!ifBranch.IsElse)
            {
                ExecValue cond = await ExecutedAsync(ifBranch.Condition, ctx).ConfigureAwait(false);
                if (!cond.IsExpression)
                    return cond;

                cond_obj = cond.ExprValue.TryDereferenceOnce(ifBranch, ifBranch.Condition);
            }

            if (ifBranch.IsElse || cond_obj.PlainValue.Cast<bool>())
                return await ExecutedAsync(ifBranch.Body, ctx).ConfigureAwait(false);
            else if (ifBranch.Next != null)
                return await ExecutedAsync(ifBranch.Next, ctx).ConfigureAwait(false);
            else
                return ExecValue.UndefinedExpression;
        }

        public ExecValue TestRun(Language.Environment env)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(TimeoutSeconds);
            Task<ExecValue> test_task = TestRunAsync(env);
            int ended_task = Task.WaitAny(Task.Delay(timeout), test_task);
            if (ended_task == 0) // timeout
                throw new TimeoutException($"Execution didn't finish in {timeout}");
            return test_task.Result;
        }
        public async Task<ExecValue> TestRunAsync(Language.Environment env)
        {
            var resolver = NameResolver.Create(env);
            if (resolver.ErrorManager.Errors.Count != 0)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ExecutionContext ctx = ExecutionContext.Create(env, this);
            FunctionDefinition main = ctx.Env.MainFunction(ctx.CreateBareComputation());

            Task<ExecValue> main_task = this.mainExecutedAsync(ctx, main);

            await ctx.Routines.CompleteWithAsync(main_task).ConfigureAwait(false);

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return main_task.Result;
        }

        private async Task<ExecValue> mainExecutedAsync(ExecutionContext ctx, FunctionDefinition main)
        {
            ObjectData program_path = null;
            ObjectData argument = null;
            ArgumentGroup variadic_group = ArgumentGroup.None();
            if (main.Parameters.Any())
            {
                program_path = await createStringAsync(ctx, CommandLineTestProgramPath).ConfigureAwait(false);
                if (!ctx.Heap.TryInc(ctx, program_path, RefCountIncReason.CommandLine, ""))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                argument = await createStringAsync(ctx, CommandLineTestArgument).ConfigureAwait(false);
                if (!ctx.Heap.TryInc(ctx, argument, RefCountIncReason.CommandLine, ""))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");

                variadic_group = ArgumentGroup.Many(argument);
                ObjectData[] args = await prepareArguments(ctx, main,
                    new[] { ArgumentGroup.Single(program_path), variadic_group }).ConfigureAwait(false);
                SetupFunctionCallData(ref ctx, null, null, args);
            }
            ExecValue result = await ExecutedAsync(main, ctx).ConfigureAwait(false);
            if (result.IsThrow)
            {
                // todo: print the stacktrace, dump memory, etc etc etc
                if (!ctx.Heap.TryRelease(ctx, result.ThrowValue, passingOutObject: null, isPassingOut: false, reason: RefCountDecReason.ReleaseExceptionFromMain, comment: ""))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                result = ExecValue.CreateThrow(null); // this is to return something for Tests
            }

            variadic_group.TryReleaseVariadic(ctx);
            if (program_path != null)
                ctx.Heap.TryRelease(ctx, program_path, null, false, RefCountDecReason.CommandLine, "");
            if (argument != null)
                ctx.Heap.TryRelease(ctx, argument, null, false, RefCountDecReason.CommandLine, "");
            return result;
        }
        internal async Task<ExecValue> ExecutedAsync(IEvaluable node, ExecutionContext ctx)
        {
            while (true)
            {
                if (this.debugMode)
                {
                    Console.Write($"[{node.DebugId}:{node.GetType().Name}] ");
                    // we might have a bug in ToString
                    Console.WriteLine($"{node}");
                }

                INameRegistryExtension.EnterNode(node, ref ctx.LocalVariables, () => new VariableRegistry(ctx.Env.Options.ScopeShadowing));

                ExecValue result;

                if (node is IfBranch if_branch)
                {
                    result = await executeAsync(ctx, if_branch).ConfigureAwait(false);
                }
                else if (node is Block block)
                {
                    result = await executeAsync(ctx, block).ConfigureAwait(false);
                }
                else if (node is Chain chain)
                {
                    result = await executeAsync(ctx, chain).ConfigureAwait(false);
                }
                else if (node is New @new)
                {
                    result = await executeAsync(ctx, @new).ConfigureAwait(false);
                }
                else if (node is FunctionDefinition func)
                {
                    result = await executeAsync(ctx, func).ConfigureAwait(false);
                }
                else if (node is VariableDeclaration decl)
                {
                    result = await executeAsync(ctx, decl).ConfigureAwait(false);
                }
                else if (node is Assignment assign)
                {
                    result = await executeAsync(ctx, assign).ConfigureAwait(false);
                }
                else if (node is NameReference name_ref)
                {
                    result = await executeAsync(ctx, name_ref).ConfigureAwait(false);
                }
                else if (node is Utf8StringLiteral str_lit)
                {
                    result = await executeAsync(ctx, str_lit).ConfigureAwait(false);
                }
                else if (node is Literal lit)
                {
                    result = await executeAsync(ctx, lit).ConfigureAwait(false);
                }
                else if (node is Return ret)
                {
                    result = await executeAsync(ctx, ret).ConfigureAwait(false);
                }
                else if (node is FunctionCall call)
                {
                    result = await executeAsync(ctx, call).ConfigureAwait(false);
                }
                else if (node is Alloc alloc)
                {
                    result = await executeAsync(ctx, alloc).ConfigureAwait(false);
                }
                else if (node is Spawn spawn)
                {
                    result = await executeAsync(ctx, spawn).ConfigureAwait(false);
                }
                else if (node is AddressOf addr)
                {
                    result = await executeAsync(ctx, addr).ConfigureAwait(false);
                }
                else if (node is BoolOperator boolOp)
                {
                    result = await executeAsync(ctx, boolOp).ConfigureAwait(false);
                }
                else if (node is IsType is_type)
                {
                    result = await executeAsync(ctx, is_type).ConfigureAwait(false);
                }
                else if (node is IsSame is_same)
                {
                    result = await executeAsync(ctx, is_same).ConfigureAwait(false);
                }
                else if (node is ReinterpretType reinterpret)
                {
                    result = await executeAsync(ctx, reinterpret).ConfigureAwait(false);
                }
                else if (node is Dereference dereference)
                {
                    result = await executeAsync(ctx, dereference).ConfigureAwait(false);
                }
                else if (node is Spread spread)
                {
                    result = await executeAsync(ctx, spread).ConfigureAwait(false);
                }
                else if (node is Throw thrower)
                {
                    result = await executeAsync(ctx, thrower).ConfigureAwait(false);
                }
                else if (node is Loop loop)
                {
                    result = await executeAsync(ctx, loop).ConfigureAwait(false);
                }
                else
                    throw new NotImplementedException($"Instruction {node.GetType().Name} is not implemented {ExceptionCode.SourceInfo()}.");

                if (node is IScope scope && ctx.LocalVariables != null)
                {
                    exitScope(ctx, scope, result);
                }

                if (node is FunctionDefinition && result.IsRecall)
                    result.RecallData.Apply(ref ctx);
                else
                    return result;
            }

        }

        private static void exitScope(ExecutionContext ctx, IScope scope, ExecValue result)
        {
            ObjectData out_obj = !result.IsExpression
                || (scope is Block block && !block.IsRead) ? null : result.ExprValue;

            foreach (Tuple<IOwnedNode, ObjectData> bindable_obj in ctx.LocalVariables.RemoveLayer())
            {
                ILocalBindable bindable = bindable_obj.Item1 as ILocalBindable;
                ObjectData obj = bindable_obj.Item2;

                ctx.Heap.TryRelease(ctx, obj, passingOutObject: out_obj, isPassingOut: false, reason: RefCountDecReason.UnwindingStack,
                    comment: $"elem: {(bindable == null ? "«¿tmp¿»" : $"{bindable}")}, scope: {scope}");
            }
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Alloc alloc)
        {
            IEntityInstance inner_type_eval;
            IEntityInstance alloc_eval;
            if (alloc.InnerTypeName.IsSelfTypeName)
            {
                if (alloc.InnerTypeName.Prefix == null)
                {
                    inner_type_eval = ctx.ThisArgument.DereferencedOnce().RunTimeTypeInstance;
                    alloc_eval = ctx.Env.Reference(inner_type_eval, alloc.InnerTypeName.OverrideMutability, viaPointer: true);
                }
                else // Self on external object
                {
                    ExecValue prefix_exec = await ExecutedAsync(alloc.InnerTypeName.Prefix, ctx).ConfigureAwait(false);
                    if (!prefix_exec.IsExpression)
                        return prefix_exec;
                    ObjectData prefix_obj = prefix_exec.ExprValue.TryDereferenceAnyMany(ctx.Env);
                    inner_type_eval = prefix_obj.RunTimeTypeInstance;
                    alloc_eval = ctx.Env.Reference(inner_type_eval, alloc.InnerTypeName.OverrideMutability, viaPointer: true);
                }
            }
            else
            {
                inner_type_eval = alloc.InnerTypeName.Evaluation.Components;
                alloc_eval = alloc.Evaluation.Components;
            }

            ExecValue result = await executeAllocObjectAsync(ctx, inner_type_eval,
                alloc_eval, null).ConfigureAwait(false);
            return result;
        }

        private static async Task<ObjectData> allocObjectAsync(ExecutionContext ctx, IEntityInstance innerTypeName,
            IEntityInstance typeName, object value)
        {
            ObjectData obj = await ObjectData.CreateInstanceAsync(ctx, innerTypeName, value).ConfigureAwait(false);

            if (innerTypeName != typeName)
            {
                return await allocateOnHeapAsync(ctx, typeName, obj).ConfigureAwait(false);
            }
            else
            {
                return obj;
            }
        }

        private static async Task<ObjectData> allocateOnHeapAsync(ExecutionContext ctx, IEntityInstance typeName, ObjectData obj)
        {
            ctx.Heap.Allocate(obj);
            return await ObjectData.CreateInstanceAsync(ctx, typeName, obj).ConfigureAwait(false);
        }

        private async Task<ExecValue> executeAllocObjectAsync(ExecutionContext ctx, IEntityInstance innerTypeName,
            IEntityInstance typeName, object value)
        {
            ObjectData obj = await allocObjectAsync(ctx, innerTypeName, typeName, value).ConfigureAwait(false);
            return ExecValue.CreateExpression(obj);
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, Literal literal)
        {
            return executeAllocObjectAsync(ctx, literal.Evaluation.Components, literal.Evaluation.Components, literal.LiteralValue);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Utf8StringLiteral literal)
        {
            // note the difference with value-literals, it goes on heap! so we cannot use its evaluation because it is pointer based
            return ExecValue.CreateExpression(await createStringAsync(ctx, literal.Value).ConfigureAwait(false));
        }

        private static Task<ObjectData> createStringAsync(ExecutionContext ctx, string value)
        {
            // note the difference with value-literals, it goes on heap! so we cannot use its evaluation because it is pointer based
            return allocObjectAsync(ctx,
                ctx.Env.Utf8StringType.InstanceOf,
                ctx.Env.Reference(ctx.Env.Utf8StringType.InstanceOf, TypeMutability.None, viaPointer: true),
                value);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Throw thrower)
        {
            ObjectData obj = (await ExecutedAsync(thrower.Expr, ctx).ConfigureAwait(false)).ExprValue;
            obj = prepareExitData(ctx, thrower, obj);
            return ExecValue.CreateThrow(obj);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Return ret)
        {
            if (ret.Expr == null)
                return ExecValue.CreateReturn(null);
            else
            {
                if (ret.TailCallOptimization != null)
                {
                    CallPreparationData call_prep = await prepareFunctionCallAsync(ret.TailCallOptimization, ctx).ConfigureAwait(false);
                    if (call_prep.Prep.Is<ExecValue>())
                        return call_prep.Prep.As<ExecValue>();
                    CallInfo call_info = call_prep.Prep.As<CallInfo>();

                    return ExecValue.CreateRecall(call_info);
                }
                else
                {
                    ExecValue exec_value = await ExecutedAsync(ret.Expr, ctx).ConfigureAwait(false);
                    if (exec_value.IsThrow)
                        return exec_value;
                    ObjectData obj = exec_value.ExprValue;
                    obj = prepareExitData(ctx, ret, obj);
                    return ExecValue.CreateReturn(obj);
                }
            }
        }

        private static ObjectData prepareExitData(ExecutionContext ctx, IFunctionExit exit, ObjectData objData)
        {
            if (exit.Expr.DereferencedCount_LEGACY != exit.DereferencingCount)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            objData = objData.Dereferenced(exit.DereferencingCount);
            if (!ctx.Env.IsPointerLikeOfType(objData.RunTimeTypeInstance))
                objData = objData.Copy();

            {
                FunctionDefinition debug_func = exit.EnclosingScope<FunctionDefinition>();
                ctx.Heap.TryIncPointer(ctx, objData, RefCountIncReason.PrepareExitData, $"{exit} from {debug_func}");
            }
            return objData;
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, AddressOf addr)
        {
            ObjectData obj = (await ExecutedAsync(addr.Expr, ctx).ConfigureAwait(false)).ExprValue;
            obj = await obj.ReferenceAsync(ctx).ConfigureAwait(false);
            return ExecValue.CreateExpression(obj);
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, BoolOperator boolOp)
        {
            ObjectData lhs_obj = (await ExecutedAsync(boolOp.Lhs, ctx).ConfigureAwait(false)).ExprValue;
            bool lhs_value = lhs_obj.PlainValue.Cast<bool>();
            switch (boolOp.Mode)
            {
                case BoolOperator.OpMode.And:
                    {
                        bool result = lhs_value;
                        if (result)
                        {
                            ObjectData rhs_obj = (await ExecutedAsync(boolOp.Rhs, ctx).ConfigureAwait(false)).ExprValue;
                            bool rhs_value = rhs_obj.PlainValue.Cast<bool>();
                            result = rhs_value;
                        }
                        return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.BoolType.InstanceOf, result).ConfigureAwait(false));
                    }
                case BoolOperator.OpMode.Or:
                    {
                        bool result = lhs_value;
                        if (!result)
                        {
                            ObjectData rhs_obj = (await ExecutedAsync(boolOp.Rhs, ctx).ConfigureAwait(false)).ExprValue;
                            bool rhs_value = rhs_obj.PlainValue.Cast<bool>();
                            result = rhs_value;
                        }
                        return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.BoolType.InstanceOf, result).ConfigureAwait(false));
                    }
                default: throw new InvalidOperationException();
            }
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, IsType isType)
        {
            ExecValue lhs_exec = await ExecutedAsync(isType.Lhs, ctx).ConfigureAwait(false);
            if (!lhs_exec.IsExpression)
                return lhs_exec;
            ObjectData lhs_obj = lhs_exec.ExprValue;
            bool dummy = false;
            // todo: make something more intelligent with computation context
            IEntityInstance rhs_typename = isType.RhsTypeName.Evaluation.Components.TranslateThrough(ref dummy, ctx.Translation);
            bool result = IsType.MatchTypes(ctx.CreateBareComputation(), lhs_obj.RunTimeTypeInstance, rhs_typename);
            if (!result)
            {
                ;
            }
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.BoolType.InstanceOf,
                result).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, IsSame isSame)
        {
            ExecValue lhs_exec = await ExecutedAsync(isSame.Lhs, ctx).ConfigureAwait(false);
            if (!lhs_exec.IsExpression)
                return lhs_exec;
            ExecValue rhs_exec = await ExecutedAsync(isSame.Rhs, ctx).ConfigureAwait(false);
            if (!rhs_exec.IsExpression)
                return rhs_exec;
            if (lhs_exec.ExprValue.PlainValue == null)
                throw new Exception($"Internal error, Skila does not have null pointers {ExceptionCode.SourceInfo()}");
            if (rhs_exec.ExprValue.PlainValue == null)
                throw new Exception($"Internal error, Skila does not have null pointers {ExceptionCode.SourceInfo()}");
            bool same_ptr = lhs_exec.ExprValue.PlainValue == rhs_exec.ExprValue.PlainValue;
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.BoolType.InstanceOf,
                same_ptr).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, ReinterpretType reinterpret)
        {
            // reinterpret is used (internally) only with "is" operator and it is only for semantic check sake
            // in runtime it vanishes 
            ExecValue lhs_exec_value = await ExecutedAsync(reinterpret.Lhs, ctx).ConfigureAwait(false);
            return lhs_exec_value;
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Spread spread)
        {
            ExecValue exec_val = (await ExecutedAsync(spread.Expr, ctx).ConfigureAwait(false));
            return exec_val;
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Dereference dereference)
        {
            ExecValue val = await ExecutedAsync(dereference.Expr, ctx).ConfigureAwait(false);
            ObjectData obj = val.ExprValue.TryDereferenceAnyOnce(ctx.Env);
            return ExecValue.CreateExpression(obj);
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Spawn spawn)
        {
            CallPreparationData call_prep = await prepareFunctionCallAsync(spawn.Call, ctx).ConfigureAwait(false);
            if (call_prep.Prep.Is<ExecValue>())
                return call_prep.Prep.As<ExecValue>();
            CallInfo call_info = call_prep.Prep.As<CallInfo>();
            call_info.Apply(ref ctx);

            var ctx_clone = ctx.Clone();
            ctx.Routines.Run(ExecutedAsync(call_info.FunctionTarget, ctx_clone));
            ArgumentGroup.TryReleaseVariadic(ctx, call_prep.ArgGroups);

            return ExecValue.CreateExpression(null);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, FunctionCall call)
        {
            CallPreparationData call_prep = await prepareFunctionCallAsync(call, ctx).ConfigureAwait(false);
            if (call_prep.Prep.Is<ExecValue>())
                return call_prep.Prep.As<ExecValue>();
            CallInfo call_info = call_prep.Prep.As<CallInfo>();
            call_info.Apply(ref ctx);

            ExecValue ret = await ExecutedAsync(call_info.FunctionTarget, ctx).ConfigureAwait(false);
            ArgumentGroup.TryReleaseVariadic(ctx, call_prep.ArgGroups);
            if (ret.IsThrow)
                return ret;
            ObjectData ret_value = await handleCallResultAsync(ctx, call, ret.RetValue).ConfigureAwait(false);

            return ExecValue.CreateExpression(ret_value);
        }

        private async Task<ExecValue> callPropertyAccessorAsync(ExecutionContext ctx, NameReference name,
            Property.Accessor accessor,
             params ObjectData[] arguments)
        {
            Property prop = name.Binding.Match.Instance.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Get(accessor));
            if (this_context == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ExecValue this_exec = await ExecutedAsync(this_context, ctx).ConfigureAwait(false);
            if (this_exec.IsThrow)
                return this_exec;
            ObjectData this_value = this_exec.ExprValue.TryDereferenceAnyOnce(ctx.Env);

            FunctionDefinition prop_func = getTargetFunction(ctx, this_value, this_context.Evaluation, prop.Get(accessor));
            ExecValue ret = await callNonVariadicFunctionDirectly(ctx, prop_func, null, this_exec.ExprValue,
                arguments).ConfigureAwait(false);

            if (accessor == Property.Accessor.Setter && ret.RetValue != null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ObjectData ret_value = await handleCallResultAsync(ctx, name, ret.RetValue).ConfigureAwait(false);

            return ExecValue.CreateExpression(ret_value);
        }

        private async Task<ObjectData> prepareThisAsync(ExecutionContext ctx, IExpression thisExpr, string callName)
        {
            ExecValue this_exec = await ExecutedAsync(thisExpr, ctx).ConfigureAwait(false);
            return await prepareThisAsync(ctx, this_exec.ExprValue, callName).ConfigureAwait(false);
        }
        private async Task<ObjectData> prepareThisAsync(ExecutionContext ctx, ObjectData thisObject, string callName)
        {
            // if "this" is a value (legal at this point) we have add a reference to it because every function
            // expect to get either reference or pointer to this instance
            if (!ctx.Env.IsPointerLikeOfType(thisObject.RunTimeTypeInstance))
            {
                thisObject = await thisObject.ReferenceAsync(ctx).ConfigureAwait(false);
            }
            else if (ctx.Env.Dereference(thisObject.RunTimeTypeInstance, out IEntityInstance dummy) > 1)
                thisObject = thisObject.TryDereferenceAnyOnce(ctx.Env);

            ctx.Heap.TryInc(ctx, thisObject, RefCountIncReason.PrepareThis, $"{callName}");

            return thisObject;
        }

        private async Task<CallPreparationData> prepareFunctionCallAsync(FunctionCall call, ExecutionContext ctx)
        {
            ObjectData this_ref;
            FunctionDefinition target_func;

            if (call.IsRecall())
            {
                // this is a speedup, but above all it is a solution for recurent closure calls which are NOT resolved
                // in regard to this meta-parameter in compile time
                if (ctx.ThisArgument == null)
                    this_ref = null;
                else
                    this_ref = await prepareThisAsync(ctx, ctx.ThisArgument, $"{call}").ConfigureAwait(false);
                target_func = call.Resolution.TargetFunction;
            }
            else
            {
                ObjectData this_value;

                if (call.Resolution.MetaThisArgument != null)
                {
                    ExecValue this_exec = await ExecutedAsync(call.Resolution.MetaThisArgument.Expression, ctx).ConfigureAwait(false);
                    if (this_exec.IsThrow)
                        return new CallPreparationData(this_exec);

                    this_ref = await prepareThisAsync(ctx, this_exec.ExprValue, $"{call}").ConfigureAwait(false);
                    this_value = this_ref.TryDereferenceAnyOnce(ctx.Env);
                }
                else
                {
                    this_ref = null;
                    this_value = null;
                }

                target_func = getTargetFunction(ctx, this_value, call.Resolution.MetaThisArgument?.Evaluation,
                    call.Resolution.TargetFunction);

                if (target_func == null)
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            }

            ObjectData[] arguments;
            ArgumentGroup[] args_buffer = new ArgumentGroup[call.Resolution.TargetFunction.Parameters.Count];
            foreach (var param in call.Resolution.TargetFunction.Parameters.Skip(call.Resolution.IsExtendedCall ? 1 : 0))
            {
                var buff = new List<ObjectData>();

                bool is_spread = false;
                foreach (FunctionArgument arg in call.Resolution.GetArguments(param.Index))
                {
                    if (arg.IsSpread)
                        is_spread = true;

                    ExecValue arg_exec = await ExecutedAsync(arg.Expression, ctx).ConfigureAwait(false);
                    ObjectData arg_obj = arg_exec.ExprValue;
                    if (arg_obj.TryDereferenceMany(ctx.Env, arg, arg, out ObjectData dereferenced))
                        arg_obj = dereferenced;
                    buff.Add(arg_obj);
                }

                if (!buff.Any())
                    args_buffer[param.Index] = ArgumentGroup.None();
                else if (param.IsVariadic && !is_spread)
                    args_buffer[param.Index] = ArgumentGroup.Many(buff.ToArray());
                else
                    args_buffer[param.Index] = ArgumentGroup.Single(buff.Single());
            }

            arguments = await prepareArguments(ctx, call.Resolution.TargetFunction, args_buffer).ConfigureAwait(false);


            var result = new CallInfo(target_func);

            SetupFunctionCallData(ref result, call.Name.TemplateArguments.Select(it => it.TypeName.Evaluation.Components),
                this_ref, arguments);

            return new CallPreparationData(result, args_buffer);
        }

        private async Task<ObjectData[]> prepareArguments(ExecutionContext ctx, FunctionDefinition targetFunc,
            IEnumerable<ArgumentGroup> argumentsData)
        {
            // here we create chunks for variadic arguments, we modify ref counts, etc.

            var arguments_repacked = new ObjectData[targetFunc.Parameters.Count];

            int index = -1;
            foreach (ArgumentGroup args_group in argumentsData)
            {
                ++index;

                if (args_group.IsNone)
                {
                    ;
                }
                else if (args_group.IsSingle)
                {
                    ObjectData arg_obj = args_group.Arguments.Single();
                    ctx.Heap.TryInc(ctx, arg_obj, RefCountIncReason.PrepareArgument, $"for `{targetFunc}`");
                    arguments_repacked[index] = arg_obj;
                }
                else
                {
                    // preparing arguments to be passed as one for variadic parameter
                    var chunk = new ObjectData[args_group.Arguments.Count];
                    int i = 0;
                    foreach (ObjectData arg_obj_elem in args_group.Arguments)
                    {
                        ObjectData arg_obj = arg_obj_elem;
                        ctx.Heap.TryInc(ctx, arg_obj, RefCountIncReason.PrepareArgument, $"{i} for `{targetFunc}`");
                        chunk[i] = arg_obj;
                        ++i;
                    }

                    ObjectData chunk_obj = await createChunk(ctx,
                        ctx.Env.ChunkType.GetInstance(
                        TypeMutability.None,
                        TemplateTranslation.Create(ctx.Env.ChunkType.InstanceOf,
                             targetFunc.Parameters[index].ElementTypeName.Evaluation.Components),
                        Lifetime.Timeless),
                        chunk).ConfigureAwait(false);

                    arguments_repacked[index] = await chunk_obj.ReferenceAsync(ctx).ConfigureAwait(false);
                }
            }

            return arguments_repacked;
        }

        internal static void SetupFunctionCallData<T>(ref T ctx,
            IEnumerable<IEntityInstance> templateArguments,
            ObjectData metaThis, IEnumerable<ObjectData> functionArguments)
            where T : ICallContext
        {
            ctx.TemplateArguments = templateArguments?.StoreReadOnlyList();
            ctx.ThisArgument = metaThis;
            ctx.FunctionArguments = functionArguments?.ToArray();
        }

        private static FunctionDefinition getTargetFunction(ExecutionContext ctx, ObjectData thisValue, EvaluationInfo thisEvaluation,
            FunctionDefinition targetFunc)
        {
            if (thisValue == null)
                return targetFunc;

            EntityInstance this_aggregate = thisEvaluation.Aggregate;
            // first we check if the call is made on the instance of template parameter
            if (this_aggregate.TargetType.IsTemplateParameter)
            {
                TemplateParameter template_param = this_aggregate.TargetType.TemplateParameter;
                // get the argument for given template parameter
                EntityInstance template_arg = ctx.TemplateArguments[template_param.Index].Cast<EntityInstance>();
                // and then we get the virtual table from argument to parameter
                if (!template_arg.TryGetDuckVirtualTable(this_aggregate, out VirtualTable vtable))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                // ...and once we have the mapping we get target function
                else if (!vtable.TryGetDerived(targetFunc, out targetFunc))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            }
            else if (ctx.Env.DereferencedOnce(this_aggregate, out IEntityInstance __inner_this, out bool via_pointer))
            {
                EntityInstance inner_type = __inner_this.Cast<EntityInstance>();

                // if the runtime type is exactly as the type we are hitting with function
                // then there is no need to check virtual table, because we already have desired function
                TypeDefinition target_func_owner = targetFunc.ContainingType();
                if (thisValue.RunTimeTypeInstance.TargetType == target_func_owner)
                    return targetFunc;

                bool duck_virtual = (ctx.Env.Options.InterfaceDuckTyping && inner_type.TargetType.IsInterface)
                    || inner_type.TargetType.IsProtocol;
                bool classic_virtual = targetFunc.Modifier.IsPolymorphic;

                if (duck_virtual)
                {
                    // todo: optimize it
                    // in duck mode (for now) we check all the ancestors for the correct virtual table, this is because
                    // of such cases as this
                    // let b *B = new C();
                    // let a *IA = b;
                    // on the second line the static types are "*B" -> "*IA" so the virtual table is built in type
                    // B, not C, but in runtime we have C at hand and C does not have any virtual table, because
                    // types "*C" -> "*IA" were never matched

                    bool found_duck = false;

                    {
                        IEnumerable<EntityInstance> ancestors = thisValue.RunTimeTypeInstance
                            .Concat(thisValue.RunTimeTypeInstance.TargetType.Inheritance
                            .OrderedAncestorsIncludingObject.Select(it => it.TranslateThrough(thisValue.RunTimeTypeInstance)));

                        foreach (EntityInstance ancestor in ancestors)
                        {
                            if (ancestor.TryGetDuckVirtualTable(inner_type, out VirtualTable vtable))
                            {
                                if (vtable.TryGetDerived(targetFunc, out FunctionDefinition derived))
                                {
                                    targetFunc = derived;
                                    found_duck = true;
                                    break;
                                }
                                // if it is a partial vtable, don't worry we should find proper mapping in another ancestor
                                else if (!vtable.IsPartial)
                                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                            }
                        }
                    }

                    if (!found_duck)
                        throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                }

                if (duck_virtual || targetFunc.Modifier.IsPolymorphic)
                {
                    if (thisValue.InheritanceVirtualTable.TryGetDerived(targetFunc, out FunctionDefinition derived))
                    {
                        targetFunc = derived;
                    }
                    else
                    {
                        // it is legal not having entry in current virtual table, consider such inheritance:
                        // interface IA with declaration `foo`
                        // type A : IA with implementation of `foo`
                        // type B : A 
                        // type A will have `foo` in its virtual table, but in case `B` we could have already target function
                        // (A::foo) at hand so we won't find nothing for it in virtual table because it is the last derivation
                    }
                }
            }


            return targetFunc;
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, NameReference name)
        {
            IEntity target = name.Binding.Match.Instance.Target;

            if (target is Property)
                return await callPropertyAccessorAsync(ctx, name, Property.Accessor.Getter).ConfigureAwait(false);

            if (name.Prefix != null)
            {
                if (name.Binding.Match.Instance.Target is TypeDefinition)
                {
                    ObjectData type_object = await ctx.TypeRegistry.RegisterGetAsync(ctx, name.Binding.Match.Instance).ConfigureAwait(false);
                    return ExecValue.CreateExpression(type_object);
                }
                else
                {
                    ExecValue prefix_exec = await ExecutedAsync(name.Prefix, ctx).ConfigureAwait(false);
                    ObjectData prefix_obj = prefix_exec.ExprValue;
                    prefix_obj = prefix_obj.TryDereferenceAnyMany(ctx.Env);
                    return ExecValue.CreateExpression(prefix_obj.GetField(target));
                }
            }
            else if (name.Name == NameFactory.BaseVariableName)
                return ExecValue.CreateExpression(ctx.ThisArgument);
            else if (ctx.LocalVariables.TryGet(target as ILocalBindable, out ObjectData info))
                return ExecValue.CreateExpression(info);
            else if (target is VariableDeclaration decl && decl.IsTypeContained())
            {
                var current_func = name.EnclosingScope<FunctionDefinition>();
                if (!ctx.LocalVariables.TryGet(current_func.MetaThisParameter, out ObjectData this_ref_data))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                // this is always pointer/reference so in order to get the value of "this" we have to dereference it
                ObjectData this_value = this_ref_data.DereferencedOnce();

                ObjectData field_data = this_value.GetField(target);
                return ExecValue.CreateExpression(field_data);
            }
            else if (target is TypeDefinition typedef)
            {
                ObjectData type_object = await ctx.TypeRegistry.RegisterGetAsync(ctx, name.Binding.Match.Instance).ConfigureAwait(false);
                return ExecValue.CreateExpression(type_object);
            }
            else
                throw new NotImplementedException();
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Assignment assign)
        {
            ExecValue rhs_val = await ExecutedAsync(assign.RhsValue, ctx).ConfigureAwait(false);

            if (assign.Lhs.IsSink())
            {
                ;
            }
            else
            {
                ExecValue lhs;

                if (assign.Lhs is NameReference name_ref && name_ref.Binding.Match.Instance.Target is Property prop)
                {
                    if (prop.Setter != null)
                    {
                        ObjectData rhs_obj = rhs_val.ExprValue.TryDereferenceOnce(assign, assign.RhsValue);
                        await callPropertyAccessorAsync(ctx, assign.Lhs.Cast<NameReference>(), Property.Accessor.Setter, rhs_obj).ConfigureAwait(false);
                    }
                    else if (prop.Getter.Modifier.HasAutoGenerated)
                    {
                        ObjectData rhs_obj = dereferenceWithRefInc(ctx, rhs_val.ExprValue, assign, assign.RhsValue);

                        // we are inside constructor, we have auto-getter so we assign to its field directly, it is legal
                        VariableDeclaration field = prop.Fields.Single();
                        ExecValue lhs_prefix = await ExecutedAsync(name_ref.Prefix, ctx).ConfigureAwait(false);
                        ObjectData prefix_val = lhs_prefix.ExprValue.TryDereferenceAnyOnce(ctx.Env);
                        ObjectData lhs_val = prefix_val.GetField(field);
                        lhs_val.Assign(rhs_obj);
                    }
                    else
                        throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                }
                else
                {
                    ObjectData rhs_obj = dereferenceWithRefInc(ctx, rhs_val.ExprValue, assign, assign.RhsValue);

                    lhs = await ExecutedAsync(assign.Lhs, ctx).ConfigureAwait(false);

                    ctx.Heap.TryRelease(ctx, lhs.ExprValue, passingOutObject: null, isPassingOut: false,
                        reason: RefCountDecReason.AssignmentLhsDrop,
                        comment: $"{assign.Lhs}");

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, VariableDeclaration decl)
        {
            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(await ObjectData.CreateEmptyAsync(ctx, decl.Evaluation.Aggregate).ConfigureAwait(false));
            else
            {
                rhs_val = await ExecutedAsync(decl.InitValue, ctx).ConfigureAwait(false);
                if (!rhs_val.IsExpression)
                    return rhs_val;
            }

            ObjectData rhs_obj = dereferenceWithRefInc(ctx, rhs_val.ExprValue, decl, decl.InitValue);

            ObjectData lhs_obj = rhs_obj.Copy();
            ctx.LocalVariables.Add(decl, lhs_obj);

            return rhs_val;
        }

        private static ObjectData dereferenceWithRefInc(ExecutionContext ctx, ObjectData obj, IExpression parentExpr,
            IExpression childExpr)
        {
            obj = obj.TryDereferenceOnce(parentExpr, childExpr);

            RefCountIncReason reason;
            string lhs;
            if (parentExpr is Assignment assign)
            {
                reason = RefCountIncReason.Assignment;
                lhs = $"{assign.Lhs}";
            }
            else if (parentExpr is VariableDeclaration decl)
            {
                reason = RefCountIncReason.Declaration;
                lhs = $"{decl.Name}";
            }
            else
                throw new Exception($"{ExceptionCode.SourceInfo()}");

            ctx.Heap.TryInc(ctx, obj, reason, $"`{lhs}` in `{parentExpr.EnclosingScope<FunctionDefinition>()}`");

            return obj;
        }

        private async Task<ObjectData> handleCallResultAsync(ExecutionContext ctx, IExpression node, ObjectData retValue)
        {
            if (retValue == null) // valid, internally function "returns" void as in C, on call we replace it with Unit
            {
                return await ObjectData.CreateInstanceAsync(ctx, ctx.Env.UnitType.InstanceOf, UnitLiteral.UnitValue).ConfigureAwait(false);
            }

            // add temporary value to register, it will be unwinded with the stack
            // this is needed for cases when computation is done on fly, like
            // foo().at(0)
            // we need the result of the first call pinned down
            ctx.LocalVariables.Add(node, retValue);

            if (isScrappingOffPointer(ctx, node))
            {
                retValue = retValue.Dereferenced(node.DereferencedCount_LEGACY);
                retValue = retValue.Copy();
                for (int i = 0; i < node.DereferencedCount_LEGACY; ++i)
                    retValue = await retValue.ReferenceAsync(ctx).ConfigureAwait(false);
            }


            return retValue;

        }

        private static bool isScrappingOffPointer(ExecutionContext ctx, IExpression node)
        {
            return node.DereferencedCount_LEGACY > 1
                || (node.DereferencedCount_LEGACY > 0 && ctx.Env.IsPointerOfType(node.Evaluation.Components));
        }
    }
}
