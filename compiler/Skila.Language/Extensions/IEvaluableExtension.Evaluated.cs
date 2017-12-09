using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using System;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        public static IEntityInstance Evaluated(this INode node, ComputationContext ctx)
        {
            if (node.DebugId.Id == 3199)
            {
                ;
            }
            var evaluable = node as IEvaluable;

            if (evaluable != null && evaluable.IsComputed)
                return evaluable.Evaluation?.Components ?? EntityInstance.Joker;

            if (!ctx.AddVisited(node))
            {
                if (evaluable != null)
                {
                    if (!evaluable.IsComputed)
                        ctx.AddError(ErrorCode.CircularReference, node);
                }

                return evaluable?.Evaluation?.Components ?? EntityInstance.Joker;
            }

            INameRegistryExtension.EnterNode(node, ref ctx.EvalLocalNames, () => new NameRegistry(ctx.Env.Options.ScopeShadowing));

            {
                var bindable = node as ILocalBindable;
                if (bindable != null && bindable.Name != null)
                {
                    // hackerish exception for variables being transformed as functor fields
                    if (bindable.Owner != null)
                    {
                        if (ctx.EvalLocalNames != null)
                        {
                            if (node.DebugId.Id ==  198)
                            {
                                ;
                            }
                            if (!ctx.EvalLocalNames.Add(bindable))
                                ctx.AddError(ErrorCode.NameAlreadyExists, bindable.Name);
                            else if (bindable.Name.Name == NameFactory.SelfFunctionName
                                || bindable.Name.Name == NameFactory.BaseVariableName
                                || bindable.Name.Name == NameFactory.SuperFunctionName)
                                ctx.AddError(ErrorCode.ReservedName, bindable.Name);
                        }
                    }
                }
            }

            if (evaluable == null || !evaluable.IsComputed)
            {
                if (node is FunctionDefinition func)
                {
                    // in case of function evaluate move body of the function as last element
                    // otherwise we couldn't evaluate recursive calls
                    node.OwnedNodes.Where(it => func.UserBody != it).ForEach(it => Evaluated(it, ctx));
                    evaluable?.Evaluate(ctx);
                    func.UserBody?.Evaluated(ctx);

                    // since we computer body after main evaluation now we have to manually trigger this call
                    if (func.IsResultTypeNameInfered)
                        func.InferResultType(ctx);
                }
                else
                {
                    node.OwnedNodes.ForEach(it => Evaluated(it, ctx));
                    evaluable?.Evaluate(ctx);
                }
            }

            if (node is IScope && ctx.EvalLocalNames != null)
            {
                foreach (IBindable bindable in ctx.EvalLocalNames.RemoveLayer())
                {
                    // do not report regular variables here, because we have to make difference between
                    // reading and assigning, loop label does not have such distinction
                    // and function parameter is always assigned
                    if (bindable is IAnchor
                        || (bindable is FunctionParameter param && param.Name.Name != NameFactory.ThisVariableName))
                        ctx.AddError(ErrorCode.BindableNotUsed, bindable);
                }
            }

            ctx.RemoveVisited(node);

            return evaluable?.Evaluation?.Components ?? EntityInstance.Joker;
        }
    }

}
