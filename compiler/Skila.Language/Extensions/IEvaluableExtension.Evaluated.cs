using NaiveLanguageTools.Common;
using Skila.Language.Semantics;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        public static IEntityInstance Evaluated(this INode node, ComputationContext ctx)
        {
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
            if (ctx.EvalLocalNames == null && node is IExecutableScope)
                ctx.EvalLocalNames = new NameRegistry();

            ctx.EvalLocalNames?.AddLayer(node as IScope);

            {
                var bindable = node as IBindable;
                if (bindable != null && bindable.Name != null)
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
                    // do not report variables here, because we have to make difference between
                    // reading and assigning, loop label does not have such distinction
                    if (bindable is IAnchor)
                        ctx.AddError(ErrorCode.BindableNotUsed, bindable);
                }
            }

            ctx.RemoveVisited(node);

            return evaluable?.Evaluation ?? EntityInstance.Joker;
        }
    }

}
