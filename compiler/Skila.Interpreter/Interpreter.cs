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

namespace Skila.Interpreter
{
    public sealed class Interpreter : IInterpreter
    {
        private readonly bool debugMode;

        public Interpreter(bool debugMode = false)
        {
            this.debugMode = debugMode;
        }
        private async Task<ExecValue> executeAsync(FunctionDefinition func, ExecutionContext ctx)
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

            TypeDefinition owner_type = func.OwnerType();

            if (func.IsNewConstructor())
            {
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(false);
            }
            // not all methods in plain types (like Int,Double) are native
            // so we check just a function modifier
            else if (func.Modifier.HasNative)
            {
                // meta-this is always passed as reference or pointer, so we can blindly dereference it
                ObjectData this_value = ctx.ThisArgument.Dereference();

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
                    if (func.IsInitConstructor()
                        && func.Parameters.Count == 1 && func.Parameters.Single().Name.Name == NameFactory.ChunkSizeConstructorParameter)
                    {
                        IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                        ObjectData size_obj = ctx.FunctionArguments.Single();
                        int size = size_obj.PlainValue.Cast<int>();
                        ObjectData[] chunk = (await Task.WhenAll(Enumerable.Range(0, size).Select(_ => ObjectData.CreateEmptyAsync(ctx, elem_type))).ConfigureAwait(false)).ToArray();
                        this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, new Chunk(chunk)).ConfigureAwait(false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func == ctx.Env.ChunkAtSet)
                    {
                        ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                        int idx = idx_obj.PlainValue.Cast<int>();
                        Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                        chunk[idx] = ctx.GetArgument(func, NameFactory.PropertySetterValueParameter);
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func == ctx.Env.ChunkAtGet)
                    {
                        ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                        int idx = idx_obj.PlainValue.Cast<int>();
                        Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                        return ExecValue.CreateReturn(chunk[idx]);
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
                    if (func.Name.Name == NameFactory.AddOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, this_int + arg_int).ConfigureAwait(false));
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

                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        bool result = await channel.SendAsync(arg).ConfigureAwait(false);
                        return ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, result).ConfigureAwait(false));
                    }
                    else if (func.Name.Name == NameFactory.ChannelReceive)
                    {
                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        EntityInstance channel_type = this_value.RunTimeTypeInstance;
                        IEntityInstance value_type = channel_type.TemplateArguments.Single();
                        // we have to compute Skila Option type (not C# one we use for C# channel type)
                        EntityInstance option_type = ctx.Env.OptionType.GetInstance(new[] { value_type }, overrideMutability: false, translation: null);

                        Option<ObjectData> received = await channel.ReceiveAsync().ConfigureAwait(false);

                        // allocate memory for Skila option (on stack)
                        ObjectData result = await ObjectData.CreateEmptyAsync(ctx, option_type).ConfigureAwait(false);

                        // we need to call constructor for it, which takes a reference as "this"
                        ObjectData this_obj = await result.ReferenceAsync(ctx).ConfigureAwait(false);
                        if (received.HasValue)
                        {
                            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, new[] { received.Value });

                            await ExecutedAsync(ctx.Env.OptionValueConstructor, ctx).ConfigureAwait(false);
                        }
                        else
                        {
                            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, null);

                            await ExecutedAsync(ctx.Env.OptionEmptyConstructor, ctx).ConfigureAwait(false);
                        }

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

        private async Task<ExecValue> executeAsync(Block block, ExecutionContext ctx)
        {
            if (block.DebugId.Id == 161)
            {
                ;
            }
            if (block.DebugId.Id == 14637)
            {
                ;
            }
            ExecValue result = ExecValue.Undefined;

            foreach (IExpression expr in block.Instructions)
            {
                result = await ExecutedAsync(expr, ctx).ConfigureAwait(false);
                if (result.IsReturn)
                    break;
            }

            return result;
        }

        private async Task<ExecValue> executeAsync(IfBranch ifBranch, ExecutionContext ctx)
        {
            ObjectData cond_obj = null;
            if (!ifBranch.IsElse)
            {
                ExecValue cond = await ExecutedAsync(ifBranch.Condition, ctx).ConfigureAwait(false);
                if (cond.IsReturn)
                    return cond;

                cond_obj = cond.ExprValue.TryDereference(ifBranch, ifBranch.Condition);
            }

            if (ifBranch.IsElse || cond_obj.PlainValue.Cast<bool>())
                return await ExecutedAsync(ifBranch.Body, ctx).ConfigureAwait(false);
            else if (ifBranch.Next != null)
                return await ExecutedAsync(ifBranch.Next, ctx).ConfigureAwait(false);
            else
                return ExecValue.Undefined;
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
            // this method is for saving time on semantic analysis, so when you run it you know
            // the only thing is going on is execution

            ExecutionContext ctx = ExecutionContext.Create(env, this);
            Task<ExecValue> main_task = this.ExecutedAsync(main, ctx);

            ctx.Routines.CompleteWith(main_task);

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return main_task.Result;
        }

        internal async Task<ExecValue> ExecutedAsync(IEvaluable node, ExecutionContext ctx)
        {
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
                result = await executeAsync(block, ctx).ConfigureAwait(false);
            }
            else if (node is FunctionDefinition func)
            {
                result = await executeAsync(func, ctx).ConfigureAwait(false);
            }
            else if (node is VariableDeclaration decl)
            {
                result = await executeAsync(decl, ctx).ConfigureAwait(false);
            }
            else if (node is Assignment assign)
            {
                result = await executeAsync(assign, ctx).ConfigureAwait(false);
            }
            else if (node is NameReference name_ref)
            {
                result = await executeAsync(name_ref, ctx).ConfigureAwait(false);
            }
            else if (node is BoolLiteral bool_lit)
            {
                result = await executeAsync(bool_lit, ctx).ConfigureAwait(false);
            }
            else if (node is IntLiteral int_lit)
            {
                result = await executeAsync(int_lit, ctx).ConfigureAwait(false);
            }
            else if (node is StringLiteral str_lit)
            {
                result = await executeAsync(str_lit, ctx).ConfigureAwait(false);
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
                result = await executeAsync(alloc, ctx).ConfigureAwait(false);
            }
            else if (node is Spawn spawn)
            {
                result = await executeAsync(spawn, ctx).ConfigureAwait(false);
            }
            else if (node is AddressOf addr)
            {
                result = await executeAsync(addr, ctx).ConfigureAwait(false);
            }
            else if (node is BoolOperator boolOp)
            {
                result = await executeAsync(boolOp, ctx).ConfigureAwait(false);
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
                result = await executeAsync(dereference, ctx).ConfigureAwait(false);
            }
            else if (node is Spread spread)
            {
                result = await executeAsync(spread, ctx).ConfigureAwait(false);
            }
            else
                throw new NotImplementedException($"Instruction {node.GetType().Name} is not implemented {ExceptionCode.SourceInfo()}.");

            if (node is IScope && ctx.LocalVariables != null)
            {
                ObjectData out_obj = result.IsReturn || (node is Block block && !block.IsRead) ? null : result.ExprValue;

                foreach (Tuple<ILocalBindable, ObjectData> bindable_obj in ctx.LocalVariables.RemoveLayer())
                {
                    if (bindable_obj.Item1.DebugId.Id == 352)
                    {
                        ;
                    }

                    ctx.Heap.TryDec(ctx, bindable_obj.Item2, passingOut: bindable_obj.Item2 == out_obj);
                }
            }

            return result;
        }

        private async Task<ExecValue> executeAsync(Alloc alloc, ExecutionContext ctx)
        {
            ObjectData obj = await ObjectData.CreateEmptyAsync(ctx, alloc.InnerTypeName.Evaluation.Components).ConfigureAwait(false);

            if (alloc.UseHeap)
            {
                ctx.Heap.Allocate(obj);
                return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, alloc.Evaluation.Components, obj).ConfigureAwait(false));
            }
            else
            {
                return ExecValue.CreateExpression(obj);
            }
        }

        private async Task<ExecValue> executeAsync(IntLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, literal.Evaluation.Components, literal.Value).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(StringLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, literal.Evaluation.Components, literal.Value).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(Return ret, ExecutionContext ctx)
        {
            if (ret.DebugId.Id == 160)
            {
                ;
            }
            if (ret.Value == null)
                return ExecValue.CreateReturn(null);
            else
            {
                ObjectData obj = (await ExecutedAsync(ret.Value, ctx).ConfigureAwait(false)).ExprValue;
                if (ret.Value.IsDereferenced != ret.IsDereferencing)
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                if (ret.IsDereferencing)
                    obj = obj.Dereference().Copy();
                ctx.Heap.TryInc(ctx, obj);
                return ExecValue.CreateReturn(obj);
            }
        }
        private async Task<ExecValue> executeAsync(AddressOf addr, ExecutionContext ctx)
        {
            ObjectData obj = (await ExecutedAsync(addr.Expr, ctx).ConfigureAwait(false)).ExprValue;
            obj = await obj.ReferenceAsync(ctx).ConfigureAwait(false);
            return ExecValue.CreateExpression(obj);
        }
        private async Task<ExecValue> executeAsync(BoolOperator boolOp, ExecutionContext ctx)
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
        private async Task<ExecValue> executeAsync(Dereference dereference, ExecutionContext ctx)
        {
            ExecValue val = await ExecutedAsync(dereference.Expr, ctx).ConfigureAwait(false);
            ObjectData obj = val.ExprValue.TryDereference(ctx.Env);
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
            CallInfo call_info = await prepareFunctionCallAsync(call, ctx).ConfigureAwait(false);
            call_info.Apply(ref ctx);


            ExecValue ret = await ExecutedAsync(call_info.FunctionTarget, ctx).ConfigureAwait(false);
            ObjectData ret_value = ret.RetValue;

            if (ret_value == null)
            {
                ObjectData unit_obj = await ctx.TypeRegistry.RegisterGetAsync(ctx, ctx.Env.UnitType.InstanceOf).ConfigureAwait(false);
                ret_value = unit_obj.Fields.Single();
            }
            else
                ctx.Heap.TryDec(ctx, ret_value, passingOut: call.IsRead);

            return ExecValue.CreateExpression(ret_value);
        }

        private async Task<ExecValue> callPropertyGetterAsync(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 2611)
            {
                ;
            }
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Getter);
            if (this_context == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, this_context).ConfigureAwait(false);

            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, null);

            ExecValue ret = await ExecutedAsync(prop.Getter, ctx).ConfigureAwait(false);

            if (ret.RetValue != null)
                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: name.IsRead);

            return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task callPropertySetterAsync(NameReference name, IExpression value, ObjectData valueData, ExecutionContext ctx)
        {
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Setter);
            if (this_context == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, this_context).ConfigureAwait(false);

            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, new ObjectData[] { valueData });

            ExecValue ret = await ExecutedAsync(prop.Setter, ctx).ConfigureAwait(false);

            if (ret.RetValue != null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            //                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: false);

            // return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task<ObjectData> prepareThisAsync(ExecutionContext ctx, IExpression thisExpr)
        {
            ExecValue this_exec = await ExecutedAsync(thisExpr, ctx).ConfigureAwait(false);
            ObjectData this_obj = this_exec.ExprValue;
            ctx.Heap.TryInc(ctx, this_obj);

            // if "this" is a value (legal at this point) we have add a reference to it because every function
            // expect to get either reference or pointer to this instance
            //if (self == self_value)
            if (!ctx.Env.IsPointerLikeOfType(this_obj.RunTimeTypeInstance))
                this_obj = await this_obj.ReferenceAsync(ctx).ConfigureAwait(false);

            return this_obj;
        }
        private async Task<CallInfo> prepareFunctionCallAsync(FunctionCall call, ExecutionContext ctx)
        {
            if (call.DebugId.Id == 487)
            {
                ;
            }
            ObjectData this_ref;
            ObjectData this_value;
            if (call.Resolution.MetaThisArgument != null)
            {
                this_ref = await prepareThisAsync(ctx, call.Resolution.MetaThisArgument.Expression).ConfigureAwait(false);
                this_value = this_ref.TryDereference(ctx.Env);
            }
            else
            {
                this_ref = null;
                this_value = null;
            }

            var args = new ObjectData[call.Resolution.TargetFunction.Parameters.Count];
            foreach (FunctionArgument arg in call.Arguments)
            {
                ExecValue arg_exec = await ExecutedAsync(arg.Expression, ctx).ConfigureAwait(false);
                ObjectData arg_obj = arg_exec.ExprValue.TryDereference(arg, arg);
                ctx.Heap.TryInc(ctx, arg_obj);

                int idx = arg.MappedTo.Index;
                if (args[idx] != null)
                    throw new NotImplementedException();
                else
                    args[idx] = arg_obj;
            }

            FunctionDefinition target_func = getTargetFunction(ctx, call, this_value, call.Resolution.TargetFunction);

            if (target_func == null)
                throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

            var result = new CallInfo(target_func);

            SetupFunctionCallData(ref result, call.Name.TemplateArguments.Select(it => it.Evaluation.Components),
                this_ref, args);

            return result;
        }

        internal static void SetupFunctionCallData<T>(ref T ctx, IEnumerable<IEntityInstance> templateArguments,
            ObjectData metaThis, IEnumerable<ObjectData> functionArguments)
            where T : ICallContext
        {
            ctx.TemplateArguments = templateArguments?.StoreReadOnlyList();
            ctx.ThisArgument = metaThis;
            ctx.FunctionArguments = functionArguments?.ToArray();
        }

        private static FunctionDefinition getTargetFunction(ExecutionContext ctx, FunctionCall call, ObjectData thisValue,
            FunctionDefinition targetFunc)
        {
            if (call.DebugId.Id == 3257)
            {
                ;
            }
            if (call.Resolution.MetaThisArgument == null)
                return targetFunc;

            EntityInstance this_eval = call.Resolution.MetaThisArgument.Evaluation.Aggregate;
            // first we check if the call is made on the instance of template parameter
            if (this_eval.TargetType.IsTemplateParameter)
            {
                TemplateParameter template_param = this_eval.TargetType.TemplateParameter;
                // get the argument for given template parameter
                EntityInstance template_arg = ctx.TemplateArguments[template_param.Index].Cast<EntityInstance>();
                // and then we get the virtual table from argument to parameter
                if (!template_arg.TryGetDuckVirtualTable(this_eval, out VirtualTable vtable))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                // ...and once we have the mapping we get target function
                else if (!vtable.TryGetDerived(targetFunc, out targetFunc))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
            }
            else if (ctx.Env.Dereferenced(this_eval, out IEntityInstance __inner_this, out bool via_pointer))
            {
                EntityInstance inner_type = __inner_this.Cast<EntityInstance>();

                // if the runtime type is exactly as the type we are hitting with function
                // then there is no need to check virtual table, because we already have desired function
                TypeDefinition target_func_owner = targetFunc.OwnerType();
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
                        // it is legal in duck mode to have a miss, but it case of classic virtual call
                        // we simply HAVE TO have the entry for each virtual function
                        if (!duck_virtual)
                            throw new Exception($"Internal error: cannot find {targetFunc} in virtual table {ExceptionCode.SourceInfo()}");
                    }
                }
            }


            return targetFunc;
        }

        private async Task<ExecValue> executeAsync(BoolLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(await ObjectData.CreateInstanceAsync(ctx, literal.Evaluation.Components, literal.Value).ConfigureAwait(false));
        }

        private async Task<ExecValue> executeAsync(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 3459)
            {
                ;
            }
            IEntity target = name.Binding.Match.Target;

            if (target is Property)
                return await callPropertyGetterAsync(name, ctx).ConfigureAwait(false);

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
                    ObjectData prefix_obj = prefix_exec.ExprValue.TryDereference(name, name.Prefix);
                    return ExecValue.CreateExpression(prefix_obj.GetField(target));
                }
            }
            else if (name.Name == NameFactory.BaseVariableName)
                return ExecValue.CreateExpression(ctx.ThisArgument);
            else if (ctx.LocalVariables.TryGet(target as ILocalBindable, out ObjectData info))
                return ExecValue.CreateExpression(info);
            else if (target is VariableDeclaration decl && decl.IsField())
            {
                var current_func = name.EnclosingScope<FunctionDefinition>();
                if (!ctx.LocalVariables.TryGet(current_func.MetaThisParameter, out ObjectData this_ref_data))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                // this is always pointer/reference so in order to get the value of "this" we have to dereference it
                ObjectData this_value = this_ref_data.Dereference();

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

        private async Task<ExecValue> executeAsync(Assignment assign, ExecutionContext ctx)
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
                if (assign.Lhs.DebugId.Id == 352)
                {
                    ;
                }
                ExecValue lhs;
                ObjectData rhs_obj = rhs_val.ExprValue.TryDereference(assign, assign.RhsValue);
                ctx.Heap.TryInc(ctx, rhs_obj);

                if (assign.Lhs is NameReference name_ref && name_ref.Binding.Match.Target is Property)
                {
                    await callPropertySetterAsync(assign.Lhs.Cast<NameReference>(), assign.RhsValue, rhs_obj, ctx).ConfigureAwait(false);
                }
                else
                {
                    lhs = await ExecutedAsync(assign.Lhs, ctx).ConfigureAwait(false);

                    if (ctx.Heap.TryDec(ctx, lhs.ExprValue, passingOut: false))
                    {
                        ;
                    }

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private async Task<ExecValue> executeAsync(VariableDeclaration decl, ExecutionContext ctx)
        {
            if (decl.DebugId.Id == 3020)
            {
                ;
            }

            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(await ObjectData.CreateEmptyAsync(ctx, decl.Evaluation.Aggregate).ConfigureAwait(false));
            else
                rhs_val = await ExecutedAsync(decl.InitValue, ctx).ConfigureAwait(false);

            ObjectData rhs_obj = rhs_val.ExprValue.TryDereference(decl, decl.InitValue);

            ObjectData lhs_obj = rhs_obj.Copy();
            ctx.LocalVariables.Add(decl, lhs_obj);
            ctx.Heap.TryInc(ctx, lhs_obj);

            if (decl.DebugId.Id == 352)
            {
                ;
            }

            return rhs_val;
        }

    }
}
