using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static partial class IEvaluableExtension
    {
        private static void validateExecutionPath(INode node, ExecutionPath path,
            ComputationContext ctx, ref ValidationData result)
        {
            if (node is IfBranch && node.DebugId.Id == 203)
            {
                ;
            }
            foreach (IEvaluable step in path)
            {
                if (!result.UnreachableCodeFound && (result.IsTerminated))
                {
                    result.UnreachableCodeFound = true;
                    ctx.AddError(ErrorCode.UnreachableCode, step);
                }

                ValidationData val = Validated(step, ctx);

                ctx.ValAssignTracker?.UpdateMode(val.GetMode());

                result.AddStep(val);
            }

        }


        public static ValidationData Validated(this INode node, ComputationContext ctx)
        {
            if (node is IfBranch && node.DebugId.Id == 203)
            {
                ;
            }
            var evaluable = node as IEvaluable;

            if (evaluable != null && evaluable.Validation != null)
                return evaluable.Validation;

            if (!ctx.AddVisited(node))
            {
                return evaluable?.Validation;
            }

            ValidationData result = ValidationData.Create();

            INameRegistryExtension.EnterNode(node, ref ctx.ValAssignTracker, () => new AssignmentTracker(ctx.Env.Options.ScopeShadowing));

            {
                if (node is VariableDeclaration decl)
                {
                    if (decl.DebugId.Id == 2548)
                    {
                        ;
                    }
                    ctx.ValAssignTracker?.Add(decl);
                }
            }

            if (node is IExpression expr)
            {

                if (node is Loop && node.DebugId.Id == 5)
                {
                    ;
                }

                TrackingState? track_state = ctx.ValAssignTracker?.StartFlow();
                AssignmentTracker parent_tracker = ctx.ValAssignTracker;

                validateExecutionPath(node, expr.Flow.AlwaysPath, ctx, ref result);

                if (expr.Flow.ForkMaybePaths.Any())
                {
                    var branch_trackers = new List<AssignmentTracker>();
                    var branch_results = new List<ValidationData>();

                    foreach (ExecutionPath maybes in expr.Flow.ForkMaybePaths)
                    {
                        ctx.ValAssignTracker = parent_tracker?.Clone();
                        if (ctx.ValAssignTracker.DebugId.Id == 286)
                        {
                            ;
                        }
                        ValidationData branch_result = result.Clone();

                        validateExecutionPath(node, maybes, ctx, ref branch_result);

                        // time to remove "continue", because we are about to process
                        // step and post-check of the (could be) loop
                        if (node is IAnchor loop)
                            branch_result.RemoveInterruptionFor(loop, isBreak: false);

                        if (maybes == expr.Flow.ThenMaybePath && expr.Flow.ThenPostMaybes!=null)
                            validateExecutionPath(node, expr.Flow.ThenPostMaybes, ctx, ref branch_result);

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
                ctx.ValAssignTracker = parent_tracker; // restore original tracker

                foreach (IExpression sub in expr.Flow.Enumerate)
                    validateReadingValues(sub, ctx);
            }


            node.OwnedNodes.ForEach(it => Validated(it, ctx));

            if (node is IValidable verificable)
                verificable.Validate(ctx);

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

            if (node is Loop && node.DebugId.Id == 5)
            {
                ;
            }
            if (node is IScope && ctx.ValAssignTracker != null)
            {
                foreach (IEntityVariable decl in ctx.ValAssignTracker.RemoveLayer())
                {
                    ctx.AddError(ErrorCode.BindableNotUsed, decl);
                }
            }

            ctx.RemoveVisited(node);

            return (node as IEvaluable)?.Validation;
        }
    }
}
