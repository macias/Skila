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
        private ExecValue execute(FunctionDefinition func, ExecutionContext ctx)
        {
            if (func.DebugId.Id==2812)
            {
                ;
            }
            if (ctx.ThisArgument != null)
                ctx.LocalVariables.Add(func.MetaThisParameter, ctx.ThisArgument);

            {
                var parameters = new HashSet<FunctionParameter>(func.Parameters);
                if (ctx.FunctionArguments != null)
                    foreach (KeyValuePair<FunctionParameter, ObjectData> arg_value in ctx.FunctionArguments)
                    {
                        parameters.Remove(arg_value.Key);
                        ctx.LocalVariables.Add(arg_value.Key, arg_value.Value);
                    }

                foreach (FunctionParameter param in parameters)
                {
                    if (param.DefaultValue == null)
                        throw new Exception("Internal error");
                    ctx.LocalVariables.Add(param, (executed(param.DefaultValue, ctx)).ExprValue);
                }
            }

            TypeDefinition owner_type = func.OwnerType();
            if (func.IsNewConstructor())
            {
                return executeRegularFunction(func, ctx);
            }
            else if (owner_type != null && owner_type.IsPlain)
            {
                // meta-this is always passed as reference or pointer, so we can blindly dereference it
                ObjectData this_value = ctx.ThisArgument.Dereference();

                if (owner_type == ctx.Env.IntType)
                {
                    if (func.Name.Name == NameFactory.AddOperator)
                    {
                        ObjectData arg = ctx.FunctionArguments.Values.Single();
                        return ExecValue.CreateReturn(ObjectData.Create(this_value.RunTimeTypeInstance,
                            this_value.PlainValue.Cast<int>() + arg.PlainValue.Cast<int>()));
                    }
                    else if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(ObjectData.Create(this_value.RunTimeTypeInstance, 0));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Values.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type == ctx.Env.BoolType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        this_value.Assign(ObjectData.Create(this_value.RunTimeTypeInstance, false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        this_value.Assign(ctx.FunctionArguments.Values.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.NotOperator)
                    {
                        return ExecValue.CreateReturn(ObjectData.Create(this_value.RunTimeTypeInstance,
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
                        ObjectData channel_obj = ObjectData.Create(this_value.RunTimeTypeInstance, channel);
                        this_value.Assign(channel_obj);
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.ChannelSend)
                    {
                        ObjectData arg = ctx.FunctionArguments.Values.Single();

                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        bool result = channel.Send(arg);
                        return ExecValue.CreateReturn(ObjectData.Create(this_value.RunTimeTypeInstance, result));
                    }
                    else if (func.Name.Name == NameFactory.ChannelReceive)
                    {
                        Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        EntityInstance channel_type = this_value.RunTimeTypeInstance;
                        IEntityInstance value_type = channel_type.TemplateArguments.Single();
                        // we have to compute Skila Option type (not C# one we use for C# channel type)
                        EntityInstance option_type = ctx.Env.OptionType.GetInstanceOf(new[] { value_type });

                        Option<ObjectData> received = channel.Receive();

                        // allocate memory for Skila option (on stack)
                        ObjectData result = ObjectData.CreateEmpty(option_type);

                        // we need to call constructor for it, which takes a reference as "this"
                        ctx.ThisArgument = result.Reference(ctx.Env);
                        if (received.HasValue)
                        {
                            ctx.FunctionArguments = new Dictionary<FunctionParameter, ObjectData>();
                            ctx.FunctionArguments.Add(ctx.Env.OptionValueConstructor.Parameters.Single(), received.Value);
                            executed(ctx.Env.OptionValueConstructor, ctx);
                        }
                        else
                        {
                            ctx.FunctionArguments = null;
                            executed(ctx.Env.OptionEmptyConstructor, ctx);
                        }

                        // at this point Skila option is initialized so we can return it

                        return ExecValue.CreateReturn(result);
                    }
                    else
                        throw new NotImplementedException();
                }
                else
                {
                    throw new NotImplementedException();
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

            ExecValue ret = executed(func.UserBody, ctx);
            if (ctx.Env.IsVoidType(func.ResultTypeName.Evaluation))
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
                result = executed(expr, ctx);
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
                ExecValue cond = executed(ifBranch.Condition, ctx);
                if (cond.IsReturn)
                    return cond;

                cond_obj = cond.ExprValue.TryDereference(ifBranch.Condition);
            }

            if (ifBranch.IsElse || cond_obj.PlainValue.Cast<bool>())
                return executed(ifBranch.Body, ctx);
            else if (ifBranch.Next != null)
                return executed(ifBranch.Next, ctx);
            else
                return ExecValue.Undefined;
        }

        public static FunctionDefinition PrepareRun(Language.Environment env)
        {
            var resolver = NameResolver.Create(env);

            if (resolver.ErrorManager.Errors.Count != 0)
                throw new Exception("Internal error");

            return env.Root.FindEntities(NameReference.Create("main")).Single().CastFunction();
        }
        public ExecValue TestRun(Language.Environment env)
        {
            return TestRun(env, PrepareRun(env));
        }
        public ExecValue TestRun(Language.Environment env, FunctionDefinition main)
        {
            // this method is for saving time on semantic analysis, so when you run it you know
            // the only thing is going on is execution

            ExecutionContext ctx = ExecutionContext.Create(env);
            ExecValue result = this.executed(main, ctx);

            ctx.Routines.Complete();

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return result;
        }

        private ExecValue executed(IEvaluable node, ExecutionContext ctx)
        {
            if (ctx.LocalVariables == null && node is IExecutableScope)
                ctx.LocalVariables = new VariableRegistry();

            ctx.LocalVariables?.AddLayer(node as IScope);

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
            else
                throw new NotImplementedException();

            if (node is IScope && ctx.LocalVariables != null)
            {
                ObjectData out_obj = result.IsReturn || (node is Block block && !block.IsRead) ? null : result.ExprValue;

                foreach (Tuple<IBindable, ObjectData> bindable_obj in ctx.LocalVariables.RemoveLayer())
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
            var obj = ObjectData.CreateEmpty(alloc.InnerTypeName.Evaluation);

            if (alloc.UseHeap)
            {
                ctx.Heap.Allocate(obj);
                return ExecValue.CreateExpression(ObjectData.Create(alloc.Evaluation, obj));
            }
            else
            {
                return ExecValue.CreateExpression(obj);
            }
        }

        private ExecValue execute(IntLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.Create(literal.Evaluation, literal.Value));
        }

        private ExecValue execute(StringLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.Create(literal.Evaluation, literal.Value));
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
                ObjectData obj = (executed(ret.Value, ctx)).ExprValue;
                if (ret.Value.IsDereferenced)
                    obj = obj.Dereference().Copy();
                ctx.Heap.TryInc(ctx, obj);
                return ExecValue.CreateReturn(obj);
            }
        }

        private ExecValue execute(Spawn spawn, ExecutionContext ctx)
        {
            FunctionDefinition func = prepareFunctionCall(spawn.Call, ref ctx);

            var ctx_clone = ctx.Clone();
            ctx.Routines.Run(() => executed(func, ctx_clone));

            return ExecValue.CreateExpression(null);
        }

        private ExecValue execute(FunctionCall call, ExecutionContext ctx)
        {
            FunctionDefinition func = prepareFunctionCall(call, ref ctx);

            ExecValue ret = executed(func, ctx);

            if (ret.RetValue != null)
                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: call.IsRead);

            return ExecValue.CreateExpression(ret.RetValue);
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

            ctx.FunctionArguments = null;
            ctx.ThisArgument = this_ref;

            ExecValue ret = executed(prop.Getter, ctx);

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

            var args = new Dictionary<FunctionParameter, ObjectData>();
            args.Add(prop.Setter.Parameters.First(), valueData);

            ctx.FunctionArguments = args;
            ctx.ThisArgument = this_ref;

            ExecValue ret = executed(prop.Setter, ctx);

            if (ret.RetValue != null)
                throw new Exception("Internal error");
            //                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: false);

            // return ExecValue.CreateExpression(ret.RetValue);
        }

        private ObjectData prepareThis(ExecutionContext ctx, IExpression thisExpr)
        {
            ExecValue this_exec = executed(thisExpr, ctx);
            ObjectData this_obj = this_exec.ExprValue;
            ctx.Heap.TryInc(ctx, this_obj);

            // if "this" is a value (legal at this point) we have add a reference to it because every function
            // expect to get either reference or pointer to this instance
            //if (self == self_value)
            if (!ctx.Env.IsPointerLikeOfType(this_obj.RunTimeTypeInstance))
                this_obj = this_obj.Reference(ctx.Env);

            return this_obj;
        }
        private FunctionDefinition prepareFunctionCall(FunctionCall call, ref ExecutionContext ctx)
        {
            if (call.DebugId.Id == 2522)
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

            var args = new Dictionary<FunctionParameter, ObjectData>();
            foreach (FunctionArgument arg in call.Arguments)
            {
                ExecValue arg_exec = executed(arg.Expression, ctx);
                ObjectData arg_obj = arg_exec.ExprValue.TryDereference(arg);
                ctx.Heap.TryInc(ctx, arg_obj);

                args.Add(arg.MappedTo, arg_obj);
            }

            FunctionDefinition target_func = null;
            IEntity call_target = call.Resolution.TargetFunctionInstance.Target;

            if (call_target.IsFunction())
            {
                target_func = call_target.CastFunction();
                if (call.Resolution.MetaThisArgument != null)
                {
                    EntityInstance this_eval = call.Resolution.MetaThisArgument.Evaluation.Cast<EntityInstance>();
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
                        else if (!vtable.TryGetDerived(ref target_func))
                            throw new Exception("Internal error");
                    }
                    else if (ctx.Env.IsPointerLikeOfType(call.Resolution.MetaThisArgument.Evaluation))
                    {
                        bool duck_virtual = (ctx.Env.Options.InterfaceDuckTyping && target_func.OwnerType().IsInterface)
                        || target_func.OwnerType().IsProtocol;
                        bool classic_virtual = target_func.IsVirtual;

                        if (duck_virtual)
                        {
                            // we know "this" is either pointer or reference so we have to get inner type 
                            // in order to get virtual table for it
                            IEntityInstance inner_type = this_eval.TemplateArguments.Single();

                            // todo: optimize it
                            // in duck mode (for now) we check all the ancestors for the correct virtual table, this is because
                            // of such cases as this
                            // let b *B = new C();
                            // let a *IA = b;
                            // on the second line the static types are "*B" -> "*IA" so the virtual table is built in type
                            // B, not C, but in runtime we have C at hand and C does not have any virtual table, because
                            // types "*C" -> "*IA" were never matched

                            bool found_duck = false;

                            foreach (EntityInstance ancestor in this_value.RunTimeTypeInstance.TargetType.Inheritance
                                .AncestorsIncludingObject.Select(it => it.TranslateThrough(this_value.RunTimeTypeInstance))
                                .Concat(this_value.RunTimeTypeInstance))
                            {
                                if (ancestor.TryGetDuckVirtualTable(inner_type.Cast<EntityInstance>(), out VirtualTable vtable))
                                {
                                    if (!vtable.TryGetDerived(ref target_func))
                                        throw new Exception("Internal error");
                                    found_duck = true;
                                    break;
                                }
                            }

                            if (!found_duck)
                                throw new Exception("Internal error");
                        }

                        if (duck_virtual || target_func.IsVirtual)
                        {
                            if (!this_value.InheritanceVirtualTable.TryGetDerived(ref target_func))
                            {
                                // it is legal in duck mode to have a miss, but it case of classis virtual call
                                // we simply HAVE TO have the entry for each virtual function
                                if (!duck_virtual)
                                    throw new Exception("Internal error");
                            }
                        }
                    }
                }
            }

            if (target_func == null)
                throw new Exception("Internal error");

            ctx.FunctionArguments = args;
            ctx.ThisArgument = this_ref;
            ctx.TemplateArguments = call.Name.TemplateArguments.Select(it => it.Evaluation).StoreReadOnlyList();

            return target_func;
        }

        private ExecValue execute(BoolLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.Create(literal.Evaluation, literal.Value));
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
                ExecValue prefix_exec = executed(name.Prefix, ctx);
                ObjectData prefix_obj = prefix_exec.ExprValue.TryDereference(name.Prefix);
                return ExecValue.CreateExpression(prefix_obj.GetField( target));
            }
            else if (ctx.LocalVariables.TryGet(target, out ObjectData info))
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
            else if (target is TypeContainerDefinition type_container)
            {
                throw new NotImplementedException();
            }
            else
                throw new NotImplementedException();
        }

        private ExecValue execute(Assignment assign, ExecutionContext ctx)
        {
            if (assign.DebugId.Id == 2805)
            {
                ;
            }
            ExecValue rhs_val = executed(assign.RhsValue, ctx);

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
                ObjectData rhs_obj = rhs_val.ExprValue.TryDereference(assign.RhsValue);
                ctx.Heap.TryInc(ctx, rhs_obj);

                if (assign.Lhs.Cast<NameReference>().Binding.Match.Target is Property)
                {
                    callPropertySetter(assign.Lhs.Cast<NameReference>(), assign.RhsValue, rhs_obj, ctx);
                }
                else
                {
                    lhs = executed(assign.Lhs, ctx);

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private ExecValue execute(VariableDeclaration decl, ExecutionContext ctx)
        {
            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(ObjectData.CreateEmpty(decl.Evaluation));
            else
                rhs_val = executed(decl.InitValue, ctx);

            ObjectData rhs_obj = rhs_val.ExprValue.TryDereference(decl.InitValue);
            if (decl.DebugId.Id == 2560)
            {
                ;
            }

            // if (decl.InitValue != null && decl.InitValue.IsDereferenced)
            //   rhs_obj = rhs_obj.Dereference();
            ctx.Heap.TryInc(ctx, rhs_obj);

            ctx.LocalVariables.Add(decl, rhs_obj);

            if (decl.DebugId.Id == 352)
            {
                ;
            }

            return rhs_val;
        }

    }
}
