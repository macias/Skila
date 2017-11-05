using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Threading.Tasks;

namespace Skila.Interpreter
{
    public sealed class Interpreter
    {
        private const bool continueOnCapturedContext = true;

        private async Task<ExecValue> executeAsync(FunctionDefinition func, ExecutionContext ctx)
        {
            if (ctx.ThisArgument != null)
                ctx.LocalVariables.Add(func.MetaThisParameter, ctx.ThisArgument);

            {
                var parameters = new HashSet<FunctionParameter>(func.Parameters);
                if (ctx.Arguments != null)
                    foreach (KeyValuePair<FunctionParameter, ObjectData> arg_value in ctx.Arguments)
                    {
                        parameters.Remove(arg_value.Key);
                        ctx.LocalVariables.Add(arg_value.Key, arg_value.Value);
                    }

                foreach (FunctionParameter param in parameters)
                {
                    if (param.DefaultValue == null)
                        throw new Exception("Internal error");
                    ctx.LocalVariables.Add(param, (await executedAsync(param.DefaultValue, ctx).ConfigureAwait(continueOnCapturedContext)).ExprValue);
                }
            }

            TypeDefinition owner_type = func.OwnerType();
            if (func.IsNewConstructor())
            {
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (owner_type != null && owner_type.IsPlain)
            {
                if (owner_type == ctx.Env.IntType)
                {
                    if (func.Name.Name == NameFactory.AddOperator)
                    {
                        ObjectData arg = ctx.Arguments.Values.Single();
                        return ExecValue.CreateReturn(ObjectData.Create(func.MetaThisParameter.Evaluation,
                            ctx.ThisArgument.PlainValue.Cast<int>() + arg.PlainValue.Cast<int>()));
                    }
                    else if (func.IsDefaultInitConstructor())
                    {
                        ctx.ThisArgument.Assign(ObjectData.Create(func.MetaThisParameter.Evaluation, 0));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        ctx.ThisArgument.Assign(ctx.Arguments.Values.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type == ctx.Env.BoolType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        ctx.ThisArgument.Assign(ObjectData.Create(func.MetaThisParameter.Evaluation, false));
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.IsCopyInitConstructor())
                    {
                        ctx.ThisArgument.Assign(ctx.Arguments.Values.Single());
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.NotOperator)
                    {
                        return ExecValue.CreateReturn(ObjectData.Create(func.MetaThisParameter.Evaluation,
                            !ctx.ThisArgument.PlainValue.Cast<bool>()));
                    }
                    else
                        throw new NotImplementedException();
                }
                else if (owner_type == ctx.Env.ChannelType)
                {
                    if (func.IsDefaultInitConstructor())
                    {
                        ctx.Heap.AddDisposable();
                        ObjectData channel_obj = ObjectData.Create(ctx.ThisArgument.TypeInstance, Channels.Channel.Create<ObjectData>());
                        ctx.ThisArgument.Assign(channel_obj);
                        return ExecValue.CreateReturn(null);
                    }
                    else if (func.Name.Name == NameFactory.ChannelSend)
                    {
                        ObjectData arg = ctx.Arguments.Values.Single();

                        Channels.IChannel<ObjectData> channel = ctx.ThisArgument.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        bool result = await channel.SendAsync(arg).ConfigureAwait(continueOnCapturedContext);
                        return ExecValue.CreateReturn(ObjectData.Create(func.MetaThisParameter.Evaluation, result));
                    }
                    else if (func.Name.Name == NameFactory.ChannelReceive)
                    {
                        Channels.IChannel<ObjectData> channel = ctx.ThisArgument.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                        EntityInstance channel_type = ctx.ThisArgument.TypeInstance;
                        IEntityInstance value_type = channel_type.TemplateArguments.Single();
                        // we have to compute Skila Option type (not C# one we use for C# channel type)
                        EntityInstance option_type = ctx.Env.OptionType.GetInstanceOf(new[] { value_type });

                        Option<ObjectData> received = await channel.ReceiveAsync().ConfigureAwait(continueOnCapturedContext);

                        // allocate memory for Skila option (on stack)
                        ObjectData result = ObjectData.CreateEmpty(option_type);

                        // we need to call constructor for it, which takes a reference as "this"
                        ctx.ThisArgument = result.Reference(ctx.Env);
                        if (received.HasValue)
                        {
                            ctx.Arguments = new Dictionary<FunctionParameter, ObjectData>();
                            ctx.Arguments.Add(ctx.Env.OptionValueConstructor.Parameters.Single(), received.Value);
                            await executedAsync(ctx.Env.OptionValueConstructor, ctx).ConfigureAwait(continueOnCapturedContext);
                        }
                        else
                        {
                            ctx.Arguments = null;
                            await executedAsync(ctx.Env.OptionEmptyConstructor, ctx).ConfigureAwait(continueOnCapturedContext);
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
                return await executeRegularFunctionAsync(func, ctx).ConfigureAwait(continueOnCapturedContext);
            }
        }

        private async Task<ExecValue> executeRegularFunctionAsync(FunctionDefinition func, ExecutionContext ctx)
        {
            ExecValue ret = await executedAsync(func.UserBody, ctx).ConfigureAwait(continueOnCapturedContext);
            if (ctx.Env.IsVoidType(func.ResultTypeName.Evaluation))
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
                result = await executedAsync(expr, ctx).ConfigureAwait(continueOnCapturedContext);
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
                ExecValue cond = await executedAsync(ifBranch.Condition, ctx).ConfigureAwait(continueOnCapturedContext);
                if (cond.IsReturn)
                    return cond;

                cond_obj = cond.ExprValue.GetValue(ifBranch.Condition);
            }

            if (ifBranch.IsElse || cond_obj.PlainValue.Cast<bool>())
                return await executedAsync(ifBranch.Body, ctx).ConfigureAwait(continueOnCapturedContext);
            else if (ifBranch.Next != null)
                return await executedAsync(ifBranch.Next, ctx).ConfigureAwait(continueOnCapturedContext);
            else
                return ExecValue.Undefined;
        }

        public ExecValue TestRun(Language.Environment env)
        {
            var resolver = NameResolver.Create(env);

            if (resolver.ErrorManager.Errors.Count != 0)
                throw new Exception("Internal error");

            ExecutionContext ctx = ExecutionContext.Create(env);
            Task<ExecValue> exec_task = this.executedAsync(env.Root.FindEntities(NameReference.Create("main")).Single().CastFunction(),
                ctx);
            exec_task.Wait();

            if (!ctx.Heap.IsClean)
                throw new Exception("Internal error with heap");

            return exec_task.Result;
        }

        private async Task<ExecValue> executedAsync(IExpression node, ExecutionContext ctx)
        {
            if (ctx.LocalVariables == null && node is IExecutableScope)
                ctx.LocalVariables = new VariableRegistry();

            ctx.LocalVariables?.AddLayer(node as IScope);

            ExecValue result;

            if (node is IfBranch if_branch)
            {
                result = await executeAsync(if_branch, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is Block block)
            {
                result = await executeAsync(block, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is FunctionDefinition func)
            {
                result = await executeAsync(func, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is VariableDeclaration decl)
            {
                result = await executeAsync(decl, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is Assignment assign)
            {
                result = await executeAsync(assign, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is NameReference name_ref)
            {
                result = await executeAsync(name_ref, ctx).ConfigureAwait(continueOnCapturedContext);
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
                result = await executeAsync(ret, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is FunctionCall call)
            {
                result = await executeAsync(call, ctx).ConfigureAwait(continueOnCapturedContext);
            }
            else if (node is Alloc alloc)
            {
                result = execute(alloc, ctx);
            }
            else if (node is Spawn spawn)
            {
                result = await executeAsync(spawn, ctx).ConfigureAwait(continueOnCapturedContext);
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
                ObjectData obj = (await executedAsync(ret.Value, ctx).ConfigureAwait(continueOnCapturedContext)).ExprValue;
                if (ret.Value.IsDereferenced)
                    obj = obj.Dereference().Copy();
                ctx.Heap.TryInc(ctx, obj);
                return ExecValue.CreateReturn(obj);
            }
        }

        private async Task<ExecValue> callPropertyGetterAsync(NameReference name, ExecutionContext ctx)
        {
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Getter);
            if (this_context == null)
                throw new Exception("Internal error");

            ObjectData self = (await executedAsync(this_context, ctx).ConfigureAwait(continueOnCapturedContext)).ExprValue.GetValue(this_context);
            ctx.Heap.TryInc(ctx, self);

            ctx.Arguments = null;
            ctx.ThisArgument = self;

            ExecValue ret = await executedAsync(prop.Getter, ctx).ConfigureAwait(continueOnCapturedContext);

            if (ret.RetValue != null)
                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: name.IsRead);

            return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task callPropertySetterAsync(NameReference name, IExpression value, ObjectData valueData, ExecutionContext ctx)
        {
            Property prop = name.Binding.Match.Target.Cast<Property>();
            IExpression this_context = name.GetContext(prop.Setter);
            if (this_context == null)
                throw new Exception("Internal error");

            ObjectData self = (await executedAsync(this_context, ctx).ConfigureAwait(continueOnCapturedContext)).ExprValue.GetValue(this_context);
            ctx.Heap.TryInc(ctx, self);

            var args = new Dictionary<FunctionParameter, ObjectData>();
            //foreach (FunctionArgument arg in call.Arguments)
            {
                args.Add(prop.Setter.Parameters.First(), valueData);
            }

            ctx.Arguments = args;
            ctx.ThisArgument = self;

            ExecValue ret = await executedAsync(prop.Setter, ctx).ConfigureAwait(continueOnCapturedContext);

            if (ret.RetValue != null)
                throw new Exception("Internal error");
            //                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: false);

            // return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task<ExecValue> executeAsync(Spawn spawn, ExecutionContext ctx)
        {
            ctx = await prepareFunctionCallAsync(spawn.Call, ctx).ConfigureAwait(continueOnCapturedContext);

            var ctx_clone = ctx.Clone();
            ctx.Routines.Run(executedAsync(spawn.Call.Resolution.TargetInstance.Target.CastFunction(), ctx_clone));

            return ExecValue.CreateExpression(null);
        }

        private async Task<ExecValue> executeAsync(FunctionCall call, ExecutionContext ctx)
        {
            ctx = await prepareFunctionCallAsync(call, ctx).ConfigureAwait(continueOnCapturedContext);

            ExecValue ret = await executedAsync(call.Resolution.TargetInstance.Target.CastFunction(), ctx).ConfigureAwait(continueOnCapturedContext);

            if (ret.RetValue != null)
                ctx.Heap.TryDec(ctx, ret.RetValue, passingOut: call.IsRead);

            return ExecValue.CreateExpression(ret.RetValue);
        }

        private async Task<ExecutionContext> prepareFunctionCallAsync(FunctionCall call, ExecutionContext ctx)
        {
            ObjectData self;
            if (call.Resolution.MetaThisArgument != null)
            {
                ExecValue this_exec = await executedAsync(call.Resolution.MetaThisArgument.Expression, ctx).ConfigureAwait(continueOnCapturedContext);
                self = this_exec.ExprValue.GetValue(call.Name.Prefix);
                ctx.Heap.TryInc(ctx, self);
            }
            else
                self = null;

            var args = new Dictionary<FunctionParameter, ObjectData>();
            foreach (FunctionArgument arg in call.Arguments)
            {
                ExecValue arg_exec = await executedAsync(arg.Expression, ctx).ConfigureAwait(continueOnCapturedContext);
                ObjectData arg_obj = arg_exec.ExprValue.GetValue(arg);
                ctx.Heap.TryInc(ctx, arg_obj);

                args.Add(arg.MappedTo, arg_obj);
            }

            ctx.Arguments = args;
            ctx.ThisArgument = self;
            return ctx;
        }

        private ExecValue execute(BoolLiteral literal, ExecutionContext ctx)
        {
            return ExecValue.CreateExpression(ObjectData.Create(literal.Evaluation, literal.Value));
        }

        private async Task<ExecValue> executeAsync(NameReference name, ExecutionContext ctx)
        {
            if (name.DebugId.Id == 1209)
            {
                ;
            }
            IEntity target = name.Binding.Match.Target;

            if (target is Property)
                return await callPropertyGetterAsync(name, ctx).ConfigureAwait(continueOnCapturedContext);

            if (name.Prefix != null)
            {
                ExecValue prefix_exec = await executedAsync(name.Prefix, ctx).ConfigureAwait(continueOnCapturedContext);
                ObjectData prefix_obj = prefix_exec.ExprValue.GetValue(name.Prefix);
                return ExecValue.CreateExpression(prefix_obj.GetField(target));
            }
            else if (ctx.LocalVariables.TryGet(target, out ObjectData info))
                return ExecValue.CreateExpression(info);
            else if (target is VariableDeclaration decl && decl.IsField())
            {
                var current_func = name.EnclosingScope<FunctionDefinition>();
                if (!ctx.LocalVariables.TryGet(current_func.MetaThisParameter, out ObjectData this_data))
                    throw new Exception("Internal error");
                return ExecValue.CreateExpression(this_data.GetField(target));
            }
            else if (target is TypeContainerDefinition type_container)
            {
                throw new NotImplementedException();
            }
            else
                throw new NotImplementedException();
        }

        private async Task<ExecValue> executeAsync(Assignment assign, ExecutionContext ctx)
        {
            ExecValue rhs_val = await executedAsync(assign.RhsValue, ctx).ConfigureAwait(continueOnCapturedContext);

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
                ObjectData rhs_obj = rhs_val.ExprValue.GetValue(assign.RhsValue);
                ctx.Heap.TryInc(ctx, rhs_obj);

                if (assign.Lhs.Cast<NameReference>().Binding.Match.Target is Property)
                {
                    await callPropertySetterAsync(assign.Lhs.Cast<NameReference>(), assign.RhsValue, rhs_obj, ctx).ConfigureAwait(continueOnCapturedContext);
                }
                else
                {
                    lhs = await executedAsync(assign.Lhs, ctx).ConfigureAwait(continueOnCapturedContext);

                    lhs.ExprValue.Assign(rhs_obj);
                }
            }

            return rhs_val;
        }

        private async Task<ExecValue> executeAsync(VariableDeclaration decl, ExecutionContext ctx)
        {
            ExecValue rhs_val;
            if (decl.InitValue == null || decl.InitValue.IsUndef())
                rhs_val = ExecValue.CreateExpression(ObjectData.CreateEmpty(decl.Evaluation));
            else
                rhs_val = await executedAsync(decl.InitValue, ctx).ConfigureAwait(continueOnCapturedContext);

            ObjectData rhs_obj = rhs_val.ExprValue.GetValue(decl.InitValue);
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
