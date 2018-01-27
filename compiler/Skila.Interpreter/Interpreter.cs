using System;
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

namespace Skila.Interpreter
{
    public sealed class Interpreter : IInterpreter
    {
        private readonly bool debugMode;

        public Interpreter(bool debugMode = false)
        {
            this.debugMode = debugMode;
        }
        private async Task<ObjectData> createChunk(ExecutionContext ctx, EntityInstance chunkRunTimeInstance, ObjectData[] elements)
        {
            return await ObjectData.CreateInstanceAsync(ctx, chunkRunTimeInstance, new Chunk(elements)).ConfigureAwait(false);
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, FunctionDefinition func)
        {
            if (func.DebugId.Id == 170)
            {
                ;
            }
            if (ctx.ThisArgument != null)
                ctx.LocalVariables.Add(func.MetaThisParameter, ctx.ThisArgument);

            {
                for (int i = 0; i < func.Parameters.Count; ++i)
                {
                    FunctionParameter param = func.Parameters[i];
                    ObjectData arg_data = ctx.FunctionArguments[i];

                    bool added;
                    if (arg_data == null)
                        added = ctx.LocalVariables.Add(param, (await ExecutedAsync(param.DefaultValue, ctx).ConfigureAwait(false)).ExprValue);
                    else
                        added = ctx.LocalVariables.Add(param, arg_data);

                    if (!added)
                        throw new NotImplementedException();
                }
            }

            TypeDefinition owner_type = func.ContainingType();

            if (func.IsNewConstructor())
            {
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(false);
            }
            // not all methods in plain types (like Int,Double) are native
            // so we check just a function modifier
            else if (func.Modifier.HasNative)
            {
                // meta-this is always passed as reference or pointer, so we can blindly dereference it
                ObjectData this_value = func.Modifier.HasStatic ? null : ctx.ThisArgument.DereferencedOnce();

                if (owner_type.Modifier.HasEnum)
                {
                    if (func.IsInitConstructor()
                        && func.Parameters.Count == 1 && func.Parameters.Single().Name.Name == NameFactory.EnumConstructorParameter)
                    {
                        this_value.Assign(ctx.FunctionArguments.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.EqualOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components, this_int == arg_int).ConfigureAwait(false));
                        return result;
                    }
                    else if (func.Name.Name == NameFactory.ConvertFunctionName)
                    {
                        if (ctx.Env.IsIntType(func.ResultTypeName.Evaluation.Components))
                        {
                            ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components, this_value.PlainValue).ConfigureAwait(false);
                            return ExecValue.CreateReturn(result);
                        }
                        else
                            throw new NotImplementedException($"Enum func {func} is not implemented");
                    }
                    else
                        throw new NotImplementedException($"Function {func} is not implemented");
                }
                else if (owner_type == ctx.Env.UnitType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, UnitType.UnitValue).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else
                        throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                }
                else if (owner_type == ctx.Env.ChunkType)
                {
                    if (func == ctx.Env.ChunkSizeConstructor)
                    {
                        IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                        ObjectData size_obj = ctx.FunctionArguments.Single();
                        int size = size_obj.PlainValue.Cast<int>();
                        ObjectData[] chunk = (await Task.WhenAll(Enumerable.Range(0, size).Select(_ => ObjectData.CreateEmptyAsync(ctx, elem_type))).ConfigureAwait(false)).ToArray();
                        this_value.Assign(await createChunk(ctx, this_value.RunTimeTypeInstance, chunk).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func == ctx.Env.ChunkResizeConstructor)
                    {
                        if (this_value.DebugId.Id == 184694)
                        {
                            ;
                        }
                        IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                        ObjectData size_obj = ctx.FunctionArguments[0];
                        int size = size_obj.PlainValue.Cast<int>();
                        ObjectData source_obj = ctx.FunctionArguments[1];
                        if (!source_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData val_obj))
                            throw new Exception($"{ExceptionCode.SourceInfo()}");
                        Chunk source = val_obj.PlainValue.Cast<Chunk>();

                        ObjectData[] chunk = new ObjectData[size];
                        int copy_size = Math.Min(size, source.Count);
                        for (int i = 0; i < copy_size; ++i)
                        {
                            chunk[i] = source[i];
                            ctx.Heap.TryInc(ctx, source[i], "copying chunk");
                        }
                        for (int i = copy_size; i < size; ++i)
                        {
                            chunk[i] = await ObjectData.CreateEmptyAsync(ctx, elem_type).ConfigureAwait(false);
                        }
                        this_value.Assign(await createChunk(ctx, this_value.RunTimeTypeInstance, chunk).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func == ctx.Env.ChunkAtSet)
                    {
                        ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                        int idx = idx_obj.PlainValue.Cast<int>();
                        Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                        ctx.Heap.TryRelease(ctx, chunk[idx], passingOut: false, callInfo: "replacing chunk elem");
                        ObjectData arg_ref_object = ctx.GetArgument(func, NameFactory.PropertySetterValueParameter);
                        // indexer takes reference to element
                        if (!arg_ref_object.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_val))
                            throw new Exception($"{ExceptionCode.SourceInfo()}");
                        if (!ctx.Heap.TryInc(ctx, arg_val, "setting chunk elem"))
                            arg_val = arg_val.Copy();
                        chunk[idx] = arg_val;
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func == ctx.Env.ChunkAtGet)
                    {
                        ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                        int idx = idx_obj.PlainValue.Cast<int>();
                        Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                        ObjectData obj_value = chunk[idx];
                        // indexer returns reference to an element value
                        ObjectData obj_ref = await obj_value.ReferenceAsync(ctx).ConfigureAwait(false);
                        return ExecValue.CreateReturn(obj_ref);
                    }
                    else if (func == ctx.Env.ChunkCount)
                    {
                        Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                        ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                            chunk.Count).ConfigureAwait(false);
                        return ExecValue.CreateReturn(result);
                    }
                    else
                        throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                }
                else if (owner_type == ctx.Env.IntType)
                {
                    if (func == ctx.Env.IntParseStringFunction)
                    {
                        ObjectData arg_ptr = ctx.FunctionArguments.Single();
                        ObjectData arg_val = arg_ptr.DereferencedOnce();

                        string input_str = arg_val.PlainValue.Cast<string>();
                        Option<ObjectData> int_obj;
                        if (int.TryParse(input_str, out int int_val))
                            int_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.IntType.InstanceOf, int_val)
                                .ConfigureAwait(false));
                        else
                            int_obj = new Option<ObjectData>();

                        ObjectData result = await createOption(ctx, func.ResultTypeName.Evaluation.Components, int_obj).ConfigureAwait(false);
                        return ExecValue.CreateReturn(result);
                    }
                    else if (func.Name.Name == NameFactory.AddOperator)
                    {
                        int this_int = this_value.PlainValue.Cast<int>();

                        ObjectData arg = ctx.FunctionArguments.Single();
                        int arg_int = arg.PlainValue.Cast<int>();

                        ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, this_int + arg_int)
                            .ConfigureAwait(false);
                        ExecValue result = ExecValue.CreateReturn(res_value);
                        return result;
                    }
                    else if (func.Name.Name == NameFactory.MulOperator)
                    {
                        int this_int = this_value.PlainValue.Cast<int>();

                        ObjectData arg = ctx.FunctionArguments.Single();
                        int arg_int = arg.PlainValue.Cast<int>();

                        ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, this_int * arg_int)
                            .ConfigureAwait(false);
                        ExecValue result = ExecValue.CreateReturn(res_value);
                        return result;
                    }
                    else if (func.Name.Name == NameFactory.SubOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, this_int - arg_int)
                            .ConfigureAwait(false);
                        ExecValue result = ExecValue.CreateReturn(res_value);
                        return result;
                    }
                    else if (func.Name.Name == NameFactory.EqualOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components, this_int == arg_int).ConfigureAwait(false));
                        return result;
                    }
                    else if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, 0).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.ComparableCompare)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();

                        ObjectData ordering_type = await ctx.TypeRegistry.RegisterGetAsync(ctx, ctx.Env.OrderingType.InstanceOf).ConfigureAwait(false);
                        ObjectData ordering_value;
                        if (this_int < arg_int)
                            ordering_value = ordering_type.GetField(ctx.Env.OrderingLess);
                        else if (this_int > arg_int)
                            ordering_value = ordering_type.GetField(ctx.Env.OrderingGreater);
                        else
                            ordering_value = ordering_type.GetField(ctx.Env.OrderingEqual);

                        ExecValue result = ExecValue.CreateReturn(ordering_value);
                        return result;
                    }
                    else
                        throw new NotImplementedException($"Function {func} is not implemented");
                }
                else if (owner_type == ctx.Env.BoolType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, false).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.NotOperator)
                    {
                        return ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance,
                            !this_value.PlainValue.Cast<bool>()).ConfigureAwait(false));
                    }
                    else
                        throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                }
                else if (owner_type == ctx.Env.DateType)
                {
                    if (func == ctx.Env.DateDayOfWeekGetter)
                    {
                        ObjectData year_obj = this_value.GetField(ctx.Env.DateYearField);
                        int year = year_obj.PlainValue.Cast<int>();
                        ObjectData month_obj = this_value.GetField(ctx.Env.DateMonthField);
                        int month = month_obj.PlainValue.Cast<int>();
                        ObjectData day_obj = this_value.GetField(ctx.Env.DateDayField);
                        int day = day_obj.PlainValue.Cast<int>();

                        int day_of_week = (int)(new DateTime(year, month, day).DayOfWeek);

                        ObjectData ret_obj = await ObjectData.CreateInstanceAsync(ctx,
                            func.ResultTypeName.Evaluation.Components, day_of_week).ConfigureAwait(false);

                        return ExecValue.CreateReturn(ret_obj);
                    }
                    else
                    {
                        throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                    }
                }
                else if (owner_type == ctx.Env.ChannelType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        Channels.IChannel<ObjectData> channel = Channels.Channel.Create<ObjectData>();
                        ctx.Heap.TryAddDisposable(channel);
                        ObjectData channel_obj = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, channel).ConfigureAwait(false);
                        this_value.Assign(channel_obj);
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.ChannelSend)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        if (!ctx.Env.IsPointerOfType(arg.RunTimeTypeInstance))
                            arg = arg.Copy();

                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        bool result = await channel.SendAsync(arg).ConfigureAwait(false);
                        return ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, result).ConfigureAwait(false));
                    }
                    else if (func.Name.Name == NameFactory.ChannelReceive)
                    {
                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        EntityInstance channel_type = this_value.RunTimeTypeInstance;
                        IEntityInstance value_type = channel_type.TemplateArguments.Single();

                        Option<ObjectData> received = await channel.ReceiveAsync().ConfigureAwait(false);

                        // we have to compute Skila Option type (not C# one we use for C# channel type)
                        EntityInstance option_type = ctx.Env.OptionType.GetInstance(new[] { value_type }, overrideMutability: MutabilityFlag.ConstAsSource,
                            translation: null);
                        ObjectData result = await createOption(ctx, option_type, received).ConfigureAwait(false);

                        // at this point Skila option is initialized so we can return it

                        return ExecValue.CreateReturn(result);
                    }
                    else
                        throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                }
                else
                {
                    throw new NotImplementedException($"{owner_type}");
                }
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
            if (block.DebugId.Id == 161)
            {
                ;
            }

            return executeAsync(ctx, block.Instructions);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, IEnumerable<IExpression> instructions)
        {
            ExecValue result = ExecValue.UndefinedExpression;

            foreach (IExpression expr in instructions)
            {
                result = await ExecutedAsync(expr, ctx).ConfigureAwait(false);
                if (result.Mode != DataMode.Expression)
                    break;
            }

            return result;
        }
        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Loop loop)
        {
            if (loop.DebugId.Id == 161)
            {
                ;
            }
            ExecValue result = await executeAsync(ctx, loop.Init).ConfigureAwait(false);
            if (result.Mode != DataMode.Expression)
                return result;

            result = ExecValue.UndefinedExpression;

            while (true)
            {
                ctx.LocalVariables.AddLayer(loop);

                result = await iterateLoopAsync(ctx, loop).ConfigureAwait(false);
                exitScope(ctx, loop, result);

                if (result.Mode != DataMode.Expression
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
                if (result.Mode != DataMode.Expression)
                    return result;
                bool cond = result.ExprValue.PlainValue.Cast<bool>();
                if (!cond)
                    return result;
            }

            result = await executeAsync(ctx, loop.Body).ConfigureAwait(false);
            if (result.Mode != DataMode.Expression)
                return result;

            result = await executeAsync(ctx, loop.PostStep).ConfigureAwait(false);
            if (result.Mode != DataMode.Expression)
                return result;

            if (loop.PostCondition != null)
            {
                result = await ExecutedAsync(loop.PostCondition, ctx).ConfigureAwait(false);
                if (result.Mode != DataMode.Expression)
                    return result;
                bool cond = result.ExprValue.PlainValue.Cast<bool>();
                if (!cond)
                    return result;
            }

            return ExecValue.UndefinedExpression;
        }
        private async Task<ExecValue> executeAsync(IfBranch ifBranch, ExecutionContext ctx)
        {
            ObjectData cond_obj = null;
            if (!ifBranch.IsElse)
            {
                ExecValue cond = await ExecutedAsync(ifBranch.Condition, ctx).ConfigureAwait(false);
                if (cond.Mode == DataMode.Return)
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

        public static FunctionDefinition PrepareRun(Language.Environment env)
        {
            var resolver = NameResolver.Create(env);

            if (resolver.ErrorManager.Errors.Count != 0)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            return env.Root.FindEntities(NameReference.Create("main"), EntityFindMode.ScopeLimited).Single().Target.CastFunction();
        }
        public ExecValue TestRun(Language.Environment env)
        {
            return TestRun(env, PrepareRun(env));
        }
        public ExecValue TestRun(Language.Environment env, FunctionDefinition main)
        {
            //if (env.Options.GetEnabledProperties().Except(new[] { nameof(IOptions.DiscardingAnyExpressionDuringTests) }).Any())
            //  throw new InvalidOperationException();

            // this method is for saving time on semantic analysis, so when you run it you know
            // the only thing is going on is execution

            ExecutionContext ctx = ExecutionContext.Create(env, this);
            Task<ExecValue> main_task = this.mainExecutedAsync(main, ctx);

            ctx.Routines.CompleteWith(main_task);

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return main_task.Result;
        }

        private async Task<ExecValue> mainExecutedAsync(IEvaluable node, ExecutionContext ctx)
        {
            ExecValue result = await ExecutedAsync(node, ctx).ConfigureAwait(false);
            if (result.Mode == DataMode.Throw)
            {
                // todo: print the stacktrace, dump memory, etc etc etc
                if (!ctx.Heap.TryRelease(ctx, result.ThrowValue, passingOut: false, callInfo: $"release exception from main"))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                result = ExecValue.CreateThrow(null); // this is to return something for Tests
            }
            return result;
        }
        internal async Task<ExecValue> ExecutedAsync(IEvaluable node, ExecutionContext ctx)
        {
            if (node.DebugId.Id == 1206)
            {
                ;
            }

            if (this.debugMode)
                Console.WriteLine($"[{node.DebugId.Id}:{node.GetType().Name}] {node}");

            INameRegistryExtension.EnterNode(node, ref ctx.LocalVariables, () => new VariableRegistry(ctx.Env.Options.ScopeShadowing));

            ExecValue result;

            if (node is IfBranch if_branch)
            {
                result = await executeAsync(if_branch, ctx).ConfigureAwait(false);
            }
            else if (node is Block block)
            {
                result = await executeAsync(ctx, block).ConfigureAwait(false);
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
                result = await executeAsync(name_ref, ctx).ConfigureAwait(false);
            }
            else if (node is BoolLiteral bool_lit)
            {
                result = await executeAsync(ctx, bool_lit).ConfigureAwait(false);
            }
            else if (node is IntLiteral int_lit)
            {
                result = await executeAsync(int_lit, ctx).ConfigureAwait(false);
            }
            else if (node is StringLiteral str_lit)
            {
                result = await executeAsync(ctx, str_lit).ConfigureAwait(false);
            }
            else if (node is Return ret)
            {
                result = await executeAsync(ret, ctx).ConfigureAwait(false);
            }
            else if (node is FunctionCall call)
            {
                result = await executeAsync(call, ctx).ConfigureAwait(false);
            }
            else if (node is Alloc alloc)
            {
                result = await executeAsync(ctx, alloc).ConfigureAwait(false);
            }
            else if (node is Spawn spawn)
            {
                result = await executeAsync(spawn, ctx).ConfigureAwait(false);
            }
            else if (node is AddressOf addr)
            {
                result = await executeAsync(ctx, addr).ConfigureAwait(false);
            }
            else if (node is BoolOperator boolOp)
            {
                result = await executeAsync(ctx, boolOp).ConfigureAwait(false);
            }
            else if (node is IsType isType)
            {
                result = await executeAsync(isType, ctx).ConfigureAwait(false);
            }
            else if (node is ReinterpretType reinterpret)
            {
                result = await executeAsync(reinterpret, ctx).ConfigureAwait(false);
            }
            else if (node is Dereference dereference)
            {
                result = await executeAsync(ctx, dereference).ConfigureAwait(false);
            }
            else if (node is Spread spread)
            {
                result = await executeAsync(spread, ctx).ConfigureAwait(false);
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

            return result;
        }

        private static void exitScope(ExecutionContext ctx, IScope scope, ExecValue result)
        {
            ObjectData out_obj = result.Mode != DataMode.Expression
                || (scope is Block block && !block.IsRead) ? null : result.ExprValue;

            foreach (Tuple<ILocalBindable, ObjectData> bindable_obj in ctx.LocalVariables.RemoveLayer())
            {
                if (bindable_obj.Item1.Name.Name == "chicken")
                {
                    ;
                }
                if (bindable_obj.Item1.DebugId.Id == 5055)
                {
                    ;
                }

                ctx.Heap.TryRelease(ctx, bindable_obj.Item2, passingOut: bindable_obj.Item2 == out_obj,
                    callInfo: $"unwinding {bindable_obj.Item1} from stack of {scope}");
            }
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, Alloc alloc)
        {
            return allocObjectAsync(ctx, alloc.InnerTypeName.Evaluation.Components, alloc.Evaluation.Components, null);
        }

        private async Task<ExecValue> allocObjectAsync(ExecutionContext ctx, IEntityInstance innerTypeName, IEntityInstance typeName, object value)
        {
            ObjectData obj = await ObjectData.CreateInstanceAsync(ctx, innerTypeName, value).ConfigureAwait(false);

            if (innerTypeName != typeName)
            {
                ctx.Heap.Allocate(obj);
                return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, typeName, obj).ConfigureAwait(false));
            }
            else
            {
                return ExecValue.CreateExpression(obj);
            }
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, BoolLiteral literal)
        {
            return allocObjectAsync(ctx, literal.Evaluation.Components, literal.Evaluation.Components, literal.Value);
        }

        private Task<ExecValue> executeAsync(IntLiteral literal, ExecutionContext ctx)
        {
            return allocObjectAsync(ctx, literal.Evaluation.Components, literal.Evaluation.Components, literal.Value);
        }

        private Task<ExecValue> executeAsync(ExecutionContext ctx, StringLiteral literal)
        {
            return allocObjectAsync(ctx, ctx.Env.StringType.InstanceOf, literal.Evaluation.Components, literal.Value);
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Throw thrower)
        {
            ObjectData obj = (await ExecutedAsync(thrower.Expr, ctx).ConfigureAwait(false)).ExprValue;
            obj = prepareExitData(ctx, thrower, obj);
            return ExecValue.CreateThrow(obj);
        }

        private async Task<ExecValue> executeAsync(Return ret, ExecutionContext ctx)
        {
            if (ret.DebugId.Id == 160)
            {
                ;
            }
            if (ret.Expr == null)
                return ExecValue.CreateReturn(null);
            else
            {
                ObjectData obj = (await ExecutedAsync(ret.Expr, ctx).ConfigureAwait(false)).ExprValue;
                obj = prepareExitData(ctx, ret, obj);
                return ExecValue.CreateReturn(obj);
            }
        }

        private static ObjectData prepareExitData(ExecutionContext ctx, IFunctionExit exit, ObjectData objData)
        {
            if (exit.Expr.IsDereferenced != exit.IsDereferencing)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            if (exit.IsDereferencing)
                objData = objData.DereferencedOnce();
            if (!ctx.Env.IsPointerLikeOfType(objData.RunTimeTypeInstance))
                objData = objData.Copy();
            ctx.Heap.TryInc(ctx, objData, $"{nameof(prepareExitData)} {exit}");
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
        private async Task<ExecValue> executeAsync(IsType isType, ExecutionContext ctx)
        {
            ObjectData lhs_obj = (await ExecutedAsync(isType.Lhs, ctx).ConfigureAwait(false)).ExprValue;
            // todo: make something more intelligent with computation context
            TypeMatch match = lhs_obj.RunTimeTypeInstance.MatchesTarget(ComputationContext.CreateBare(ctx.Env),
                isType.RhsTypeName.Evaluation.Components,
                allowSlicing: false);
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.BoolType.InstanceOf,
                match == TypeMatch.Same || match == TypeMatch.Substitute).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(ReinterpretType reinterpret, ExecutionContext ctx)
        {
            // reinterpret is used (internally) only with "is" operator and it is only for semantic check sake
            // in runtime it vanishes 
            ExecValue lhs_exec_value = await ExecutedAsync(reinterpret.Lhs, ctx).ConfigureAwait(false);
            return lhs_exec_value;
        }
        private async Task<ExecValue> executeAsync(Spread spread, ExecutionContext ctx)
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
        private async Task<ExecValue> executeAsync(Spawn spawn, ExecutionContext ctx)
        {
            CallInfo call_info = await prepareFunctionCallAsync(spawn.Call, ctx).ConfigureAwait(false);
            call_info.Apply(ref ctx);

            var ctx_clone = ctx.Clone();
            ctx.Routines.Run(ExecutedAsync(call_info.FunctionTarget, ctx_clone));

            return ExecValue.CreateExpression(null);
        }

        private async Task<ExecValue> executeAsync(FunctionCall call, ExecutionContext ctx)
        {
            if (call.DebugId.Id == 27230)
            {
                ;
            }
            CallInfo call_info = await prepareFunctionCallAsync(call, ctx).ConfigureAwait(false);
            call_info.Apply(ref ctx);


            ExecValue ret = await ExecutedAsync(call_info.FunctionTarget, ctx).ConfigureAwait(false);
            if (ret.Mode == DataMode.Throw)
                return ret;
            ObjectData ret_value = ret.RetValue;

            if (ret_value == null)
            {
                ObjectData unit_obj = await ctx.TypeRegistry.RegisterGetAsync(ctx, ctx.Env.UnitType.InstanceOf).ConfigureAwait(false);
                ret_value = unit_obj.Fields.Single().Value;
            }
            else
                ctx.Heap.TryRelease(ctx, ret_value, passingOut: call.IsRead, callInfo: $"drop ret {call}");

            return ExecValue.CreateExpression(ret_value);
        }

        private async Task<ExecValue> callPropertyGetterAsync(ExecutionContext ctx,NameReference name)
        {
            if (name.DebugId.Id == 2611)
            {
                ;
            }
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Getter);
            if (this_context == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, this_context, $"prop-get {name}").ConfigureAwait(false);
            ObjectData this_value = this_ref.TryDereferenceAnyOnce(ctx.Env);

            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, null);

            FunctionDefinition getter = getTargetFunction(ctx, this_value, this_context.Evaluation, prop.Getter);
            ExecValue ret = await ExecutedAsync(getter, ctx).ConfigureAwait(false);

            if (ret.RetValue != null)
                ctx.Heap.TryRelease(ctx, ret.RetValue, passingOut: name.IsRead, callInfo: $"drop ret {name}");

            return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task callPropertySetterAsync(ExecutionContext ctx,NameReference name, IExpression value, ObjectData rhsValue)
        {
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Setter);
            if (this_context == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, this_context, $"prop-set {name}").ConfigureAwait(false);

            rhsValue = prepareArgument(ctx, rhsValue);
            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, new ObjectData[] { rhsValue });

            ExecValue ret = await ExecutedAsync(prop.Setter, ctx).ConfigureAwait(false);

            if (ret.RetValue != null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
        }

        private async Task<ObjectData> prepareThisAsync(ExecutionContext ctx, IExpression thisExpr, string callName)
        {
            ExecValue this_exec = await ExecutedAsync(thisExpr, ctx).ConfigureAwait(false);
            ObjectData this_ref = this_exec.ExprValue;

            // if "this" is a value (legal at this point) we have add a reference to it because every function
            // expect to get either reference or pointer to this instance
            if (!ctx.Env.IsPointerLikeOfType(this_ref.RunTimeTypeInstance))
            {
                this_ref = await this_ref.ReferenceAsync(ctx).ConfigureAwait(false);
            }
            else if (ctx.Env.Dereference(this_ref.RunTimeTypeInstance, out IEntityInstance dummy) > 1)
                this_ref = this_ref.TryDereferenceAnyOnce(ctx.Env);

            ctx.Heap.TryInc(ctx, this_ref, $"{nameof(prepareThisAsync)} {thisExpr} -> {callName}");

            return this_ref;
        }
        private async Task<CallInfo> prepareFunctionCallAsync(FunctionCall call, ExecutionContext ctx)
        {
            if (call.DebugId.Id == 127717)
            {
                ;
            }
            ObjectData this_ref;
            ObjectData this_value;
            if (call.Resolution.MetaThisArgument != null)
            {
                this_ref = await prepareThisAsync(ctx, call.Resolution.MetaThisArgument.Expression, $"call {call}").ConfigureAwait(false);
                this_value = this_ref.TryDereferenceAnyOnce(ctx.Env);
            }
            else
            {
                this_ref = null;
                this_value = null;
            }

            var args = new ObjectData[call.Resolution.TargetFunction.Parameters.Count];
            foreach (var param in call.Resolution.TargetFunction.Parameters)
            {
                IReadOnlyCollection<FunctionArgument> arguments = call.Resolution.GetArguments(param.Index).StoreReadOnly();
                if (!arguments.Any())
                    continue;

                if (arguments.Count == 1)
                {
                    FunctionArgument arg = arguments.Single();
                    ExecValue arg_exec = await ExecutedAsync(arg.Expression, ctx).ConfigureAwait(false);
                    ObjectData arg_obj = arg_exec.ExprValue;
                    if (arg_obj.TryDereferenceMany(ctx.Env, arg, arg, out ObjectData dereferenced))
                        arg_obj = dereferenced;
                    else
                        ctx.Heap.TryInc(ctx, arg_obj, $"{nameof(prepareFunctionCallAsync)} non-variadic {arg}");

                    arg_obj = prepareArgument(ctx, arg_obj);

                    args[param.Index] = arg_obj;
                }
                else
                {
                    // preparing arguments to be passed as one for variadic parameter
                    var chunk = new ObjectData[arguments.Count];
                    int i = 0;
                    foreach (FunctionArgument arg in arguments)
                    {
                        ExecValue arg_exec = await ExecutedAsync(arg.Expression, ctx).ConfigureAwait(false);
                        ObjectData arg_obj = arg_exec.ExprValue.TryDereferenceOnce(arg, arg);
                        ctx.Heap.TryInc(ctx, arg_obj, $"{nameof(prepareFunctionCallAsync)} variadic {i} {arg}");

                        chunk[i] = arg_obj;
                        ++i;
                    }

                    ObjectData chunk_obj = await createChunk(ctx,
                        ctx.Env.ChunkType.GetInstance(new[] { param.ElementTypeName.Evaluation.Components }, MutabilityFlag.ConstAsSource, null),
                        chunk).ConfigureAwait(false);
                    args[param.Index] = await chunk_obj.ReferenceAsync(ctx).ConfigureAwait(false);
                }
            }

            FunctionDefinition target_func = getTargetFunction(ctx, this_value, call.Resolution.MetaThisArgument?.Evaluation,
                call.Resolution.TargetFunction);

            if (target_func == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            var result = new CallInfo(target_func);

            SetupFunctionCallData(ref result, call.Name.TemplateArguments.Select(it => it.Evaluation.Components),
                this_ref, args);

            return result;
        }

        private static ObjectData prepareArgument(ExecutionContext ctx, ObjectData argument)
        {
            if (!ctx.Env.IsPointerLikeOfType(argument.RunTimeTypeInstance))
            {
                // since we passing argument by value we need to make a copy, 
                // because otherwise unwinding the stack would destroy the original
                argument = argument.Copy(); 
            }

            return argument;
        }

        internal static void SetupFunctionCallData<T>(ref T ctx, IEnumerable<IEntityInstance> templateArguments,
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

                    foreach (EntityInstance ancestor in thisValue.RunTimeTypeInstance.TargetType.Inheritance
                        .AncestorsIncludingObject.Select(it => it.TranslateThrough(thisValue.RunTimeTypeInstance))
                        .Concat(thisValue.RunTimeTypeInstance))
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

                    if (!found_duck)
                        throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                }

                if (duck_virtual || targetFunc.Modifier.IsPolymorphic)
                {
                    if (thisValue.InheritanceVirtualTable.TryGetDerived(targetFunc, out FunctionDefinition derived))
                    {
                        targetFunc = derived;
                        if (thisValue.RunTimeTypeInstance.TargetType.DebugId.Id == 213)
                        {
                            ;
                        }
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

        private async Task<ExecValue> executeAsync(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 3459)
            {
                ;
            }
            IEntity target = name.Binding.Match.Target;

            if (target is Property)
                return await callPropertyGetterAsync(ctx,name).ConfigureAwait(false);

            if (name.Prefix != null)
            {
                if (name.Binding.Match.Target is TypeDefinition)
                {
                    ObjectData type_object = await ctx.TypeRegistry.RegisterGetAsync(ctx, name.Binding.Match).ConfigureAwait(false);
                    return ExecValue.CreateExpression(type_object);
                }
                else
                {
                    ExecValue prefix_exec = await ExecutedAsync(name.Prefix, ctx).ConfigureAwait(false);
                    ObjectData prefix_obj = prefix_exec.ExprValue;
                    if (prefix_obj.TryDereferenceMany(ctx.Env, name, name.Prefix, out ObjectData dereferenced))
                        prefix_obj = dereferenced;
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
                ObjectData type_object = await ctx.TypeRegistry.RegisterGetAsync(ctx, name.Binding.Match).ConfigureAwait(false);
                return ExecValue.CreateExpression(type_object);
            }
            else
                throw new NotImplementedException();
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, Assignment assign)
        {
            if (assign.DebugId.Id == 3100)
            {
                ;
            }
            ExecValue rhs_val = await ExecutedAsync(assign.RhsValue, ctx).ConfigureAwait(false);

            if (assign.Lhs.IsSink())
            {
            }
            else
            {
                if (assign.Lhs.DebugId.Id == 5871)
                {
                    ;
                }
                ExecValue lhs;
                ObjectData rhs_obj = rhs_val.ExprValue.TryDereferenceOnce(assign, assign.RhsValue);
                ctx.Heap.TryInc(ctx, rhs_obj, $"rhs-assignment {assign}");

                if (assign.Lhs is NameReference name_ref && name_ref.Binding.Match.Target is Property)
                {
                    await callPropertySetterAsync(ctx,assign.Lhs.Cast<NameReference>(), assign.RhsValue, rhs_obj).ConfigureAwait(false);
                }
                else
                {
                    lhs = await ExecutedAsync(assign.Lhs, ctx).ConfigureAwait(false);

                    ctx.Heap.TryRelease(ctx, lhs.ExprValue, passingOut: false, callInfo: $"drop lhs, assignment {assign}");

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private async Task<ExecValue> executeAsync(ExecutionContext ctx, VariableDeclaration decl)
        {
            if (decl.DebugId.Id == 2495)
            {
                ;
            }

            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(await ObjectData.CreateEmptyAsync(ctx, decl.Evaluation.Aggregate).ConfigureAwait(false));
            else
            {
                rhs_val = await ExecutedAsync(decl.InitValue, ctx).ConfigureAwait(false);
                if (rhs_val.Mode != DataMode.Expression)
                    return rhs_val;
            }

            ObjectData rhs_obj = rhs_val.ExprValue.TryDereferenceOnce(decl, decl.InitValue);

            ObjectData lhs_obj = rhs_obj.Copy();
            ctx.LocalVariables.Add(decl, lhs_obj);
            ctx.Heap.TryInc(ctx, lhs_obj, $"lhs-decl {decl}");

            if (decl.DebugId.Id == 352)
            {
                ;
            }

            return rhs_val;
        }

        private async Task<ObjectData> createOption(ExecutionContext ctx, IEntityInstance optionType, Option<ObjectData> option)
        {
            // allocate memory for Skila option (on stack)
            ObjectData result = await ObjectData.CreateEmptyAsync(ctx, optionType).ConfigureAwait(false);

            // we need to call constructor for it, which takes a reference as "this"
            ObjectData this_obj = await result.ReferenceAsync(ctx).ConfigureAwait(false);
            if (option.HasValue)
            {
                SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, new[] { option.Value });

                await ExecutedAsync(ctx.Env.OptionValueConstructor, ctx).ConfigureAwait(false);
            }
            else
            {
                SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, null);

                await ExecutedAsync(ctx.Env.OptionEmptyConstructor, ctx).ConfigureAwait(false);
            }

            return result;
        }

    }
}
