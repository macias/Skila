using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        public static IEntityInstance Evaluated(this INode node, ComputationContext ctx)
        {
            if (node.DebugId.Id== 5707)
            {
                ;
            }
            var evaluable = node as IEvaluable;

            if (evaluable != null && evaluable.IsComputed)
                return evaluable.Evaluation ?? EntityInstance.Joker;

            if (!ctx.AddVisited(node))
            {
                if (evaluable != null)
                {
                    if (!evaluable.IsComputed)
                        ctx.AddError(ErrorCode.CircularReference, node);
                }

                return evaluable?.Evaluation ?? EntityInstance.Joker;
            }

            // if we hit a scope that has a notion of flow (function, block) and there is no name registry set
            // then create it
            if (node is TypeContainerDefinition)
                ctx.EvalLocalNames = null;
            else if (ctx.EvalLocalNames == null)
            {
                if (node is IExecutableScope)
                    ctx.EvalLocalNames = new NameRegistry();
            }
            else if (node is FunctionDefinition func && !func.IsLambda)
            {
                ctx.EvalLocalNames = new NameRegistry();
            }

            ctx.EvalLocalNames?.AddLayer(node as IScope);

            {
                var bindable = node as IBindable;
                if (bindable != null && bindable.Name != null
                    // hackerish exception for variables being transformed as functor fields
                    && bindable.Owner!=null)
                {
                    if (ctx.EvalLocalNames != null)
                    {
                        if (!ctx.EvalLocalNames.Add(bindable))
                            ctx.AddError(ErrorCode.NameAlreadyExists, bindable.Name);
                    }
                }
            }

            if (evaluable == null || !evaluable.IsComputed)
            {
                node.OwnedNodes.ForEach(it => Evaluated(it, ctx));
                evaluable?.Evaluate(ctx);
            }

            if (node is IScope && ctx.EvalLocalNames != null)
            {
                foreach (IBindable bindable in ctx.EvalLocalNames.RemoveLayer())
                {
                    // do not report regular variables here, because we have to make difference between
                    // reading and assigning, loop label does not have such distinction
                    // and function parameter is always assigned
                    if (bindable is IAnchor || (bindable is FunctionParameter param && param.UsageRequired))
                        ctx.AddError(ErrorCode.BindableNotUsed, bindable);
                }
            }

            ctx.RemoveVisited(node);

            return evaluable?.Evaluation ?? EntityInstance.Joker;
        }
    }

}
