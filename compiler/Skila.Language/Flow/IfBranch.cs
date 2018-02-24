using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using System;
using Skila.Language.Entities;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class IfBranch : Node, IExpression, IExecutableScope
    {
        public static IfBranch CreateIf(IExpression condition, IExpression body, IfBranch next = null)
        {
            return CreateIf(condition, new[] { body }, next);
        }
        public static IfBranch CreateIf(IExpression condition, IEnumerable<IExpression> body, IfBranch next = null)
        {
            return new IfBranch(condition, body, next);
        }
        public static IfBranch CreateElse(IExpression body, IfBranch next = null)
        {
            return CreateElse(new[] { body }, next);
        }
        public static IfBranch CreateElse(IEnumerable<IExpression> body, IfBranch next = null)
        {
            return new IfBranch(condition: null, body: body, next: next);
        }

        private IEnumerable<IfBranch> branches => new[] { this }.Concat(Next == null ? Enumerable.Empty<IfBranch>() : Next.branches);

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        private Option<ExpressionReadMode> readMode;
        public ExpressionReadMode ReadMode => this.readMode.Value;
        public bool IsElse => this.Condition == null;
        private IExpression condition;
        // anything declared in condition is visible in `then` and `else` branch, but not outside entire `if`
        public IExpression Condition => this.condition;
        public Block Body { get; }
        public IfBranch Next { get; }

        public IEnumerable<IExpression> localNodes => new IExpression[] { Condition, Body }.Where(it => it != null);
        public override IEnumerable<INode> OwnedNodes => localNodes.Concat(Next).Where(it => it != null);
        public ExecutionFlow Flow => this.IsElse ? ExecutionFlow.CreateElse(Body, Next) : ExecutionFlow.CreateFork(Condition, Body, Next);

        public bool IsComputed => this.Evaluation != null;
        public int DereferencingCount { get; set; }
        public int DereferencedCount_LEGACY { get; set; }
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        private IfBranch(IExpression condition, IEnumerable<IExpression> body, IfBranch next)
        {
            this.condition = condition;
            // we have to postpone calculating read-mode because the last instruction can be function call
            // and it is resolved only after finding its target
            if (!body.Any())//@@@
                body = new[] { NameFactory.UnitTypeReference(ExpressionReadMode.OptionalUse) };
            this.Body = Block.Create((block) => block.Instructions.Last().ReadMode, body);
            this.Next = next;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string result = (IsElse ? "else" : $"if {Condition} then") + " ...";
            return result;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return this.Condition == node || this.ReadMode == ExpressionReadMode.ReadRequired
                    || this.Owner.Cast<IExpression>().IsReadingValueOfNode(this);
        }

        public void Validate(ComputationContext ctx)
        {
            if (Next != null && this.IsElse)
                ctx.ErrorManager.AddError(ErrorCode.MiddleElseBranch, this);
        }
        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.readMode = new Option<ExpressionReadMode>(this.Body.ReadMode);

                if (!this.branches.Any(it => it.IsElse))
                    this.readMode = new Option<ExpressionReadMode>(ExpressionReadMode.CannotBeRead);
                else if (branches.Any(it => it.ReadMode == ExpressionReadMode.ReadRequired))
                    this.readMode = new Option<ExpressionReadMode>(ExpressionReadMode.ReadRequired);
                else if (!this.IsElse)
                    this.readMode = new Option<ExpressionReadMode>(ExpressionReadMode.OptionalUse);


                if (ReadMode == ExpressionReadMode.CannotBeRead)
                    this.Evaluation = ctx.Env.UnitEvaluation;
                else
                {
                    IEntityInstance eval = this.Body.Evaluation.Components;
                    IEntityInstance aggregate = this.Body.Evaluation.Aggregate;

                    if (Next != null
                        // it is legal to have some branch "unreadable", for example: 
                        // if true then 5 else throw new Exception();
                        && Next.ReadMode != ExpressionReadMode.CannotBeRead
                        && !computeLowestCommonAncestor(ctx, ref eval, ref aggregate))
                    {
                        eval = ctx.Env.UnitType.InstanceOf;
                        aggregate = ctx.Env.UnitType.InstanceOf;
                        readMode = new Option<ExpressionReadMode>(ExpressionReadMode.CannotBeRead);
                    }


                    this.Evaluation = new EvaluationInfo(eval, aggregate.Cast<EntityInstance>());
                    this.DataTransfer(ctx, ref this.condition, ctx.Env.BoolType.InstanceOf);

                }
            }
        }

        private bool computeLowestCommonAncestor(ComputationContext ctx, ref IEntityInstance eval, ref IEntityInstance aggregate)
        {
            if (this.DebugId.Id == 28)
            {
                ;
            }

            if (!TypeMatcher.LowestCommonAncestor(ctx, eval, Next.Evaluation.Components, out eval))
            {
                return false;
            }
            else if (!TypeMatcher.LowestCommonAncestor(ctx, aggregate, Next.Evaluation.Aggregate, out aggregate))
            {
                return false;
            }
            else
            {
                foreach (IEvaluable part in new IEvaluable[] { Body, Next })
                {
                    if (part.Evaluation.Components.MatchesTarget(ctx, eval, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false)) == TypeMatch.No)
                    {
                        return false;
                    }
                    if (part.Evaluation.Aggregate.MatchesTarget(ctx, aggregate, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false)) == TypeMatch.No)
                    {
                        return false;
                    }
                }
            }


            return true;
        }

        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
    }
}
