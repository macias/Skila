using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using System.Linq;
using System.Diagnostics;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        public static IEntityInstance Evaluated(this INode node, ComputationContext ctx, EvaluationCall evalCall)
        {
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

            // todo: this is hacky, redesign this
            // some evaluation jumps out from their natural scope (like function) and evaluate "external" fields
            // to prevent keeping local names in this external context we added flag to mark the natural, nested call
            // or cross jump, in the latter case we reset the local names registry
            if (evalCall == EvaluationCall.AdHocCrossJump)
                ctx.EvalLocalNames = null;

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
                            if (!ctx.EvalLocalNames.Add(bindable))
                                ctx.AddError(ErrorCode.NameAlreadyExists, bindable.Name);
                            else if (bindable.Name.Name == NameFactory.RecurFunctionName
                                || bindable.Name.Name == NameFactory.BaseVariableName
                                || bindable.Name.Name == NameFactory.SuperFunctionName)
                                ctx.AddError(ErrorCode.ReservedName, bindable.Name);
                        }
                    }
                }
            }

            if (evaluable == null || !evaluable.IsComputed)
            {
                if (node is ICustomComputable custom)
                    custom.CustomEvaluate(ctx);
                else
                {
                    node.OwnedNodes.ForEach(it => Evaluated(it, ctx, EvaluationCall.Nested));
                    (node as IComputable)?.Evaluate(ctx);
                }
            }

            if (node is IScope && ctx.EvalLocalNames != null)
            {
                foreach (LocalInfo bindable_info in ctx.EvalLocalNames.RemoveLayer())
                {
                    if (bindable_info.Bindable is VariableDeclaration decl)
                    {
                        if (!bindable_info.Read)
                        {
                            ctx.AddError(ErrorCode.BindableNotUsed, decl.Name);
                        }
                    }
                    else if (!bindable_info.Used)
                    {
                        // do not report regular variables here, because we have to make difference between
                        // reading and assigning, loop label does not have such distinction
                        // and function parameter is always assigned
                        if (bindable_info.Bindable is IAnchor
                        || (bindable_info.Bindable is FunctionParameter param && param.UsageMode == ExpressionReadMode.ReadRequired))
                            ctx.AddError(ErrorCode.BindableNotUsed, bindable_info.Bindable.Name);
                    }
                }
            }

            ctx.RemoveVisited(node);

            return evaluable?.Evaluation?.Components ?? EntityInstance.Joker;
        }
    }

}
