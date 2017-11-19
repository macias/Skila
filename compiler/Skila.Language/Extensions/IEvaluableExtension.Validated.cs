using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        public static ValidationData Validated(this INode node, ComputationContext ctx)
        {
            var evaluable = node as IEvaluable;

            if (evaluable != null && evaluable.Validation != null)
                return evaluable.Validation;

            if (!ctx.AddVisited(node))
            {
                return evaluable?.Validation;
            }

            ValidationData result = ValidationData.Create();

            if (ctx.ValAssignTracker == null && node is IExecutableScope)
                ctx.ValAssignTracker = new VariableTracker();

            ctx.ValAssignTracker?.AddLayer(node as IScope);

            {
                if (node is VariableDeclaration decl)
                    ctx.ValAssignTracker?.Add(decl);
            }

            if (node is IExpression expr)
            {

                if (node.DebugId.Id == 2678)
                {
                    ;
                }

                TrackingState? track_state = ctx.ValAssignTracker?.StartFlow();

                validateExecutionPath(node, expr.Flow.AlwaysPath, ctx, ref result);

                VariableTracker parent_tracker = ctx.ValAssignTracker;

                if (expr.Flow.MaybePaths.Any())
                {
                    var branch_trackers = new List<VariableTracker>();
                    var branch_results = new List<ValidationData>();

                    foreach (IEnumerable<IExpression> maybes in expr.Flow.MaybePaths)
                    {
                        ctx.ValAssignTracker = parent_tracker?.Clone();
                        ValidationData branch_result = result.Clone();

                        validateExecutionPath(node, maybes, ctx, ref branch_result);

                        // time to remove "continue", because we are about to process
                        // step and post-check of the (could be) loop
                        if (node is IAnchor loop)
                            branch_result.RemoveInterruptionFor(loop, isBreak: false);

                        validateExecutionPath(node, expr.Flow.PostMaybes, ctx, ref branch_result);

                        parent_tracker?.Import(ctx.ValAssignTracker);

                        if (!branch_result.IsTerminated)
                            branch_trackers.Add(ctx.ValAssignTracker);

                        branch_results.Add(branch_result);
                    }

                    if (expr.Flow.ExhaustiveMaybes)
                        parent_tracker?.Combine(branch_trackers);

                    if (node.DebugId.Id == 234)
                    {
                        ;
                    }

                    result.Combine(branch_results);
                }

                parent_tracker?.EndTracking(track_state.Value);

                foreach (IExpression sub in expr.Flow.Enumerate)
                    validateReadingValues(sub, ctx);
            }


            node.OwnedNodes.ForEach(it => Validated(it, ctx));

            if (evaluable != null)
                evaluable.Validate(ctx);
            else if (node is IVerificable verificable)
                verificable.Verify(ctx);

            if (node is IFunctionExit)
                result.AddExit();
            else if (node is LoopInterrupt loop_interrupt)
                result.AddInterruption(loop_interrupt);
            else if (node is IAnchor loop)
                result.RemoveInterruptionFor(loop, isBreak: true);
            else if (node is FunctionDefinition)
                result = ValidationData.Create(); // clear it, function scope should not leak any info outside

            if (evaluable != null)
                evaluable.Validation = result;

            if (node is IScope && ctx.ValAssignTracker != null)
            {
                foreach (VariableDeclaration decl in ctx.ValAssignTracker.RemoveLayer())
                {
                    ctx.AddError(ErrorCode.BindableNotUsed, decl);
                }
            }

            ctx.RemoveVisited(node);

            return (node as IEvaluable)?.Validation;
        }
    }
}
