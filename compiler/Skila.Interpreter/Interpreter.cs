using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Flow;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System.Collections.Generic;
using NaiveLanguageTools.Common;

namespace Skila.Interpreter
{
    public sealed class Interpreter : IInterpreter
    {
        private readonly bool debugMode;

        public Interpreter(bool debugMode = false)
        {
            this.debugMode = debugMode;
        }
        private ExecValue execute(FunctionDefinition func, ExecutionContext ctx)
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
                        added = ctx.LocalVariables.Add(param, (Executed(param.DefaultValue, ctx)).ExprValue);
                    else
                        added = ctx.LocalVariables.Add(param, arg_data);

                    if (!added)
                        throw new NotImplementedException();
                }
            }

            TypeDefinition owner_type = func.OwnerType();

            if (func.IsNewConstructor())
            {
                return executeRegularFunction(func, ctx);
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
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type==ctx.Env.UnitType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, UnitType.UnitValue));
                        return ExecValue.CreateReturn(null);
                    }
                    else
                        throw new NotImplementedException();

                }
                else if (owner_type == ctx.Env.IntType)
                {
                    if (func.Name.Name == NameFactory.AddOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ExecValue result = ExecValue.CreateReturn(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, this_int + arg_int));
                        return result;
                    }
                    else if (func.Name.Name == NameFactory.EqualOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();
                        int this_int = this_value.PlainValue.Cast<int>();
                        int arg_int = arg.PlainValue.Cast<int>();
                        ExecValue result = ExecValue.CreateReturn(ObjectData.CreateInstance(ctx, func.ResultTypeName.Evaluation.Components, this_int == arg_int));
                        return result;
                    }
                    else if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, 0));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type == ctx.Env.BoolType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.NotOperator)
                    {
                        return ExecValue.CreateReturn(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance,
                            !this_value.PlainValue.Cast<bool>()));
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type == ctx.Env.ChannelType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        Channels.IChannel<ObjectData> channel = Channels.Channel.Create<ObjectData>();
                        ctx.Heap.TryAddDisposable(channel);
                        ObjectData channel_obj = ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, channel);
                        this_value.Assign(channel_obj);
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.ChannelSend)
                    {
                        ObjectData arg = ctx.FunctionArguments.Single();

                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        bool result = channel.Send(arg);
                        return ExecValue.CreateReturn(ObjectData.CreateInstance(ctx, this_value.RunTimeTypeInstance, result));
                    }
                    else if (func.Name.Name == NameFactory.ChannelReceive)
                    {
                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        EntityInstance channel_type = this_value.RunTimeTypeInstance;
                        IEntityInstance value_type = channel_type.TemplateArguments.Single();
                        // we have to compute Skila Option type (not C# one we use for C# channel type)
                        EntityInstance option_type = ctx.Env.OptionType.GetInstanceOf(new[] { value_type }, overrideMutability: false);

                        Option<ObjectData> received = channel.Receive();

                        // allocate memory for Skila option (on stack)
                        ObjectData result = ObjectData.CreateEmpty(ctx, option_type);

                        // we need to call constructor for it, which takes a reference as "this"
                        ObjectData this_obj = result.Reference(ctx);
                        if (received.HasValue)
                        {
                            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, new[] { received.Value });

                            Executed(ctx.Env.OptionValueConstructor, ctx);
                        }
                        else
                        {
                            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_obj, null);

                            Executed(ctx.Env.OptionEmptyConstructor, ctx);
                        }

                        // at this point Skila option is initialized so we can return it

                        return ExecValue.CreateReturn(result);
                    }
                    else
                        throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException($"{owner_type}");
                }
            }
            else
            {
                return executeRegularFunction(func, ctx);
            }
        }

        private ExecValue executeRegularFunction(FunctionDefinition func, ExecutionContext ctx)
        {
            if (func.IsDeclaration)
                throw new Exception("Internal error");

            ExecValue ret = Executed(func.UserBody, ctx);
            if (ctx.Env.IsVoidType(func.ResultTypeName.Evaluation.Components) 
                || ctx.Env.IsUnitType(func.ResultTypeName.Evaluation.Components))
                return ExecValue.CreateReturn(null);
            else
                return ret;
        }

        private ExecValue execute(Block block, ExecutionContext ctx)
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
                result = Executed(expr, ctx);
                if (result.IsReturn)
                    break;
            }

            return result;
        }

        private ExecValue execute(IfBranch ifBranch, ExecutionContext ctx)
        {
            ObjectData cond_obj = null;
            if (!ifBranch.IsElse)
            {
                ExecValue cond = Executed(ifBranch.Condition, ctx);
                if (cond.IsReturn)
                    return cond;

                cond_obj = cond.ExprValue.TryDereference(ifBranch, ifBranch.Condition);
            }

            if (ifBranch.IsElse || cond_obj.PlainValue.Cast<bool>())
                return Executed(ifBranch.Body, ctx);
            else if (ifBranch.Next != null)
                return Executed(ifBranch.Next, ctx);
            else
                return ExecValue.Undefined;
        }

        public static FunctionDefinition PrepareRun(Language.Environment env)
        {
            var resolver = NameResolver.Create(env);

            if (resolver.ErrorManager.Errors.Count != 0)
                throw new Exception("Internal error");

            return env.Root.FindEntities(NameReference.Create("main"), EntityFindMode.ScopeLimited).Single().CastFunction();
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
            ExecValue result = this.Executed(main, ctx);

            ctx.Routines.Complete();

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return result;
        }

        internal ExecValue Executed(IEvaluable node, ExecutionContext ctx)
        {
            if (this.debugMode)
                Console.WriteLine($"[{node.DebugId.Id}:{node.GetType().Name}] {node}");

            INameRegistryExtension.EnterNode(node, ref ctx.LocalVariables, () => new VariableRegistry(ctx.Env.Options.ScopeShadowing));

            ExecValue result;

            if (node is IfBranch if_branch)
            {
                result = execute(if_branch, ctx);
            }
            else if (node is Block block)
            {
                result = execute(block, ctx);
            }
            else if (node is FunctionDefinition func)
            {
                result = execute(func, ctx);
            }
            else if (node is VariableDeclaration decl)
            {
                result = execute(decl, ctx);
            }
            else if (node is Assignment assign)
            {
                result = execute(assign, ctx);
            }
            else if (node is NameReference name_ref)
            {
                result = execute(name_ref, ctx);
            }
            else if (node is BoolLiteral bool_lit)
            {
                result = execute(bool_lit, ctx);
            }
            else if (node is IntLiteral int_lit)
            {
                result = execute(int_lit, ctx);
            }
            else if (node is StringLiteral str_lit)
            {
                result = execute(str_lit, ctx);
            }
            else if (node is Return ret)
            {
                result = execute(ret, ctx);
            }
            else if (node is FunctionCall call)
            {
                result = execute(call, ctx);
            }
            else if (node is Alloc alloc)
            {
                result = execute(alloc, ctx);
            }
            else if (node is Spawn spawn)
            {
                result = execute(spawn, ctx);
            }
            else if (node is AddressOf addr)
            {
                result = execute(addr, ctx);
            }
            else if (node is BoolOperator boolOp)
            {
                result = execute(boolOp, ctx);
            }
            else if (node is IsType isType)
            {
                result = execute(isType, ctx);
            }
            else if (node is ReinterpretType reinterpret)
            {
                result = execute(reinterpret, ctx);
            }
            else if (node is Dereference dereference)
            {
                result = execute(dereference, ctx);
            }
            else
                throw new NotImplementedException();

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

        private ExecValue execute(Alloc alloc, ExecutionContext ctx)
        {
            var obj = ObjectData.CreateEmpty(ctx, alloc.InnerTypeName.Evaluation.Components);

            if (alloc.UseHeap)
            {
                ctx.Heap.Allocate(obj);
                return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, alloc.Evaluation.Components, obj));
            }
            else
            {
                return ExecValue.CreateExpression(obj);
            }
        }

        private ExecValue execute(IntLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, literal.Evaluation.Components, literal.Value));
        }

        private ExecValue execute(StringLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, literal.Evaluation.Components, literal.Value));
        }

        private ExecValue execute(Return ret, ExecutionContext ctx)
        {
            if (ret.DebugId.Id == 160)
            {
                ;
            }
            if (ret.Value == null)
                return ExecValue.CreateReturn(null);
            else
            {
                ObjectData obj = (Executed(ret.Value, ctx)).ExprValue;
                if (ret.Value.IsDereferenced != ret.IsDereferencing)
                    throw new Exception("Internal error");
                if (ret.IsDereferencing)
                    obj = obj.Dereference().Clone();
                ctx.Heap.TryInc(ctx, obj);
                return ExecValue.CreateReturn(obj);
            }
        }
        private ExecValue execute(AddressOf addr, ExecutionContext ctx)
        {
            ObjectData obj = (Executed(addr.Expr, ctx)).ExprValue;
            obj = obj.Reference(ctx);
            return ExecValue.CreateExpression(obj);
        }
        private ExecValue execute(BoolOperator boolOp, ExecutionContext ctx)
        {
            ObjectData lhs_obj = (Executed(boolOp.Lhs, ctx)).ExprValue;
            bool lhs_value = lhs_obj.PlainValue.Cast<bool>();
            switch (boolOp.Mode)
            {
                case BoolOperator.OpMode.And:
                    {
                        bool result = lhs_value;
                        if (result)
                        {
                            ObjectData rhs_obj = (Executed(boolOp.Rhs, ctx)).ExprValue;
                            bool rhs_value = rhs_obj.PlainValue.Cast<bool>();
                            result = rhs_value;
                        }
                        return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, ctx.Env.BoolType.InstanceOf, result));
                    }
                case BoolOperator.OpMode.Or:
                    {
                        bool result = lhs_value;
                        if (!result)
                        {
                            ObjectData rhs_obj = (Executed(boolOp.Rhs, ctx)).ExprValue;
                            bool rhs_value = rhs_obj.PlainValue.Cast<bool>();
                            result = rhs_value;
                        }
                        return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, ctx.Env.BoolType.InstanceOf, result));
                    }
                default: throw new InvalidOperationException();
            }
        }
        private ExecValue execute(IsType isType, ExecutionContext ctx)
        {
            ObjectData lhs_obj = (Executed(isType.Lhs, ctx)).ExprValue;
            // todo: make something more intelligent with computation context
            TypeMatch match = lhs_obj.RunTimeTypeInstance.MatchesTarget(ComputationContext.CreateBare(ctx.Env),
                isType.RhsTypeName.Evaluation.Components,
                allowSlicing: false);
            return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, ctx.Env.BoolType.InstanceOf, match == TypeMatch.Same || match == TypeMatch.Substitute));
        }

        private ExecValue execute(ReinterpretType reinterpret, ExecutionContext ctx)
        {
            ObjectData lhs_obj = (Executed(reinterpret.Lhs, ctx)).ExprValue;
            ObjectData result = ObjectData.CreateInstance(ctx, reinterpret.RhsTypeName.Evaluation.Components, lhs_obj.PlainValue);
            return ExecValue.CreateExpression(result);
        }
        private ExecValue execute(Dereference dereference, ExecutionContext ctx)
        {
            ExecValue val = Executed(dereference.Expr,ctx);
            ObjectData obj = val.ExprValue.TryDereference(ctx.Env);
            return ExecValue.CreateExpression(obj);
        }
        private ExecValue execute(Spawn spawn, ExecutionContext ctx)
        {
            FunctionDefinition func = prepareFunctionCall(spawn.Call, ref ctx);

            var ctx_clone = ctx.Clone();
            ctx.Routines.Run(() => Executed(func, ctx_clone));

            return ExecValue.CreateExpression(null);
        }

        private ExecValue execute(FunctionCall call, ExecutionContext ctx)
        {
            FunctionDefinition func = prepareFunctionCall(call, ref ctx);

            ExecValue ret = Executed(func, ctx);
            ObjectData ret_value = ret.RetValue;

            if (ret_value == null)
                ret_value = ctx.TypeRegistry.Add(ctx, ctx.Env.UnitType.InstanceOf).Fields.Single();
            else
                ctx.Heap.TryDec(ctx, ret_value, passingOut: call.IsRead);

            return ExecValue.CreateExpression(ret_value);
        }

        private ExecValue callPropertyGetter(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 2611)
            {
                ;
            }
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Getter);
            if (this_context == null)
                throw new Exception("Internal error");

            ObjectData this_ref = prepareThis(ctx, this_context);

            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, null);

            ExecValue ret = Executed(prop.Getter, ctx);

            if (ret.RetValue != null)
                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: name.IsRead);

            return ExecValue.CreateExpression(ret.RetValue);
        }

        private void callPropertySetter(NameReference name, IExpression value, ObjectData valueData, ExecutionContext ctx)
        {
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Setter);
            if (this_context == null)
                throw new Exception("Internal error");

            ObjectData this_ref = prepareThis(ctx, this_context);

            SetupFunctionCallData(ref ctx, ctx.TemplateArguments, this_ref, new ObjectData[] { valueData });

            ExecValue ret = Executed(prop.Setter, ctx);

            if (ret.RetValue != null)
                throw new Exception("Internal error");
            //                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: false);

            // return ExecValue.CreateExpression(ret.RetValue);
        }

        private ObjectData prepareThis(ExecutionContext ctx, IExpression thisExpr)
        {
            ExecValue this_exec = Executed(thisExpr, ctx);
            ObjectData this_obj = this_exec.ExprValue;
            ctx.Heap.TryInc(ctx, this_obj);

            // if "this" is a value (legal at this point) we have add a reference to it because every function
            // expect to get either reference or pointer to this instance
            //if (self == self_value)
            if (!ctx.Env.IsPointerLikeOfType(this_obj.RunTimeTypeInstance))
                this_obj = this_obj.Reference(ctx);

            return this_obj;
        }
        private FunctionDefinition prepareFunctionCall(FunctionCall call, ref ExecutionContext ctx)
        {
            if (call.DebugId.Id == 487)
            {
                ;
            }
            ObjectData this_ref;
            ObjectData this_value;
            if (call.Resolution.MetaThisArgument != null)
            {
                this_ref = prepareThis(ctx, call.Resolution.MetaThisArgument.Expression);
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
                ExecValue arg_exec = Executed(arg.Expression, ctx);
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
                throw new Exception("Internal error");

            SetupFunctionCallData(ref ctx, call.Name.TemplateArguments.Select(it => it.Evaluation.Components),
                this_ref, args);

            return target_func;
        }

        internal static void SetupFunctionCallData(ref ExecutionContext ctx, IEnumerable<IEntityInstance> templateArguments,
            ObjectData metaThis, IEnumerable<ObjectData> functionArguments)
        {
            ctx.TemplateArguments = templateArguments?.StoreReadOnlyList();
            ctx.ThisArgument = metaThis;
            ctx.FunctionArguments = functionArguments?.ToArray();
        }

        private static FunctionDefinition getTargetFunction(ExecutionContext ctx, FunctionCall call, ObjectData thisValue,
            FunctionDefinition targetFunc)
        {
            if (call.DebugId.Id == 2681)
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
                    throw new Exception("Internal error");
                // ...and once we have the mapping we get target function
                else if (!vtable.TryGetDerived(targetFunc, out targetFunc))
                    throw new Exception("Internal error");
            }
            else if (ctx.Env.Dereferenced(this_eval, out IEntityInstance __inner_this, out bool via_pointer))
            {
                EntityInstance inner_type = __inner_this.Cast<EntityInstance>();

                // if the runtime type is exactly as the type we are hitting with function
                // then there is no need to check virtual table, because we already have desired function
                if (thisValue.RunTimeTypeInstance == targetFunc.OwnerType().InstanceOf)
                    return targetFunc;

                bool duck_virtual = (ctx.Env.Options.InterfaceDuckTyping && inner_type.TargetType.IsInterface)
                    || inner_type.TargetType.IsProtocol;
                bool classic_virtual = targetFunc.Modifier.IsVirtual;

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
                                throw new Exception("Internal error");
                        }
                    }

                    if (!found_duck)
                        throw new Exception("Internal error");
                }

                if (duck_virtual || targetFunc.Modifier.IsVirtual)
                {
                    if (thisValue.InheritanceVirtualTable.TryGetDerived(targetFunc, out FunctionDefinition derived))
                        targetFunc = derived;
                    else
                    {
                        // it is legal in duck mode to have a miss, but it case of classis virtual call
                        // we simply HAVE TO have the entry for each virtual function
                        if (!duck_virtual)
                            throw new Exception("Internal error");
                    }
                }
            }


            return targetFunc;
        }

        private ExecValue execute(BoolLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.CreateInstance(ctx, literal.Evaluation.Components, literal.Value));
        }

        private ExecValue execute(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 2475)
            {
                ;
            }
            IEntity target = name.Binding.Match.Target;

            if (target is Property)
                return callPropertyGetter(name, ctx);

            if (name.Prefix != null)
            {
                /*if (target.Modifier.HasStatic)
                {
                    throw new NotImplementedException();
                }
                else*/
                {
                    ExecValue prefix_exec = Executed(name.Prefix, ctx);
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
                    throw new Exception("Internal error");
                // this is always pointer/reference so in order to get the value of "this" we have to dereference it
                ObjectData this_value = this_ref_data.Dereference();

                ObjectData field_data = this_value.GetField(target);
                return ExecValue.CreateExpression(field_data);
            }
            else if (target is TypeDefinition typedef)
            {
                ObjectData type_object = ctx.TypeRegistry.Add(ctx, name.Binding.Match);
                return ExecValue.CreateExpression(type_object);
            }
            else
                throw new NotImplementedException();
        }

        private ExecValue execute(Assignment assign, ExecutionContext ctx)
        {
            if (assign.DebugId.Id == 3100)
            {
                ;
            }
            ExecValue rhs_val = Executed(assign.RhsValue, ctx);

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
                    callPropertySetter(assign.Lhs.Cast<NameReference>(), assign.RhsValue, rhs_obj, ctx);
                }
                else
                {
                    lhs = Executed(assign.Lhs, ctx);

                    if (ctx.Heap.TryDec(ctx, lhs.ExprValue, passingOut: false))
                    {
                        ;
                    }

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private ExecValue execute(VariableDeclaration decl, ExecutionContext ctx)
        {
            if (decl.DebugId.Id == 3020)
            {
                ;
            }

            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(ObjectData.CreateEmpty(ctx, decl.Evaluation.Aggregate));
            else
                rhs_val = Executed(decl.InitValue, ctx);

            ObjectData rhs_obj = rhs_val.ExprValue.TryDereference(decl, decl.InitValue);

            ObjectData lhs_obj = rhs_obj.Clone();
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
