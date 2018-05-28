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
            bool first = true;
            foreach (IEvaluable step in path)
            {
                if (!first)
                    ctx.ValAssignTracker?.ResetBranches();

                if (!result.UnreachableCodeFound && result.IsTerminated)
                {
                    result.UnreachableCodeFound = true;
                    ctx.AddError(ErrorCode.UnreachableCode, step);
                }

                ValidationData val = Validated(step, ctx);

                result.AddStep(val);

                first = false;
            }

        }

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

            INameRegistryExtension.CreateRegistry(INameRegistryExtension.EnterNode(node, ctx.ValAssignTracker),
                ref ctx.ValAssignTracker, () => new AssignmentTracker());

            if (node is Loop)
                ++ctx.ValLoopLevel;


            {
                if (node is VariableDeclaration decl)
                {
                    ctx.ValAssignTracker?.Add(decl, ctx.ValLoopLevel);
                }
            }

            if (node is IExpression expr)
            {
                AssignmentTracker parent_tracker = ctx.ValAssignTracker;

                validateExecutionPath(node, expr.Flow.AlwaysPath, ctx, ref result);

                if (expr.Flow.ThenMaybePath != null || expr.Flow.ElseMaybePath != null)
                {
                    var branch_results = new List<ValidationData>();

                    foreach (ExecutionPath maybes in expr.Flow.ForkMaybePaths)
                    {
                        {
                            AssignmentTracker cont_tracker = null;
                            if (maybes == expr.Flow.ThenMaybePath)
                                cont_tracker = parent_tracker?.ThenBranch;
                            else if (maybes == expr.Flow.ElseMaybePath)
                                cont_tracker = parent_tracker?.ElseBranch;
                            ctx.ValAssignTracker = (cont_tracker ?? parent_tracker)?.Clone();
                        }
                        ValidationData branch_result = result.Clone();

                        validateExecutionPath(node, maybes, ctx, ref branch_result);

                        // time to remove "continue", because we are about to process
                        // step and post-check of the (could be) loop
                        if (node is IAnchor loop)
                            branch_result.RemoveInterruptionFor(loop, isBreak: false);

                        if (maybes == expr.Flow.ThenMaybePath && expr.Flow.ThenPostMaybes != null)
                            validateExecutionPath(node, expr.Flow.ThenPostMaybes, ctx, ref branch_result);

                        // we need to store the branch or kill it, because few lines below we compute intersection of the branches
                        // so we don't want to intersect with some artifact from the previous tracker
                        if (maybes == expr.Flow.ThenMaybePath)
                        {
                            parent_tracker.ThenBranch = branch_result.IsTerminated 
                                ? null : (ctx.ValAssignTracker.ThenBranch ?? ctx.ValAssignTracker);
                        }
                        else if (maybes == expr.Flow.ElseMaybePath)
                        {
                            parent_tracker.ElseBranch = branch_result.IsTerminated 
                                ? null : (ctx.ValAssignTracker.ElseBranch ?? ctx.ValAssignTracker);
                        }
                        else
                            throw new Exception();

                        branch_results.Add(branch_result);
                    }

                    if (expr.Flow.ExhaustiveMaybes)
                        parent_tracker?.MergeInitializations();
                    parent_tracker?.MergeAssigments();

                    result.Combine(branch_results);
                }

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

            ctx.RemoveVisited(node);

            return (node as IEvaluable)?.Validation;
        }
    }
}
