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
        public static IfBranch CreateIf(IExpression condition, IEnumerable<IExpression> body, IfBranch next = null)
        {
            return new IfBranch(condition, body, next);
        }
        public static IfBranch CreateElse(IEnumerable<IExpression> body, IfBranch next = null)
        {
            return new IfBranch(condition: null, body: body, next: next);
        }

        private IEnumerable<IfBranch> branches => new[] { this }.Concat(Next == null ? Enumerable.Empty<IfBranch>() : Next.branches);

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public ExpressionReadMode ReadMode { get; }
        public bool IsElse => this.Condition == null;
        private IExpression condition;
        public IExpression Condition => this.condition;
        public Block Body { get; }
        public IfBranch Next { get; }

        public IEnumerable<IExpression> localNodes => new IExpression[] { Condition, Body }.Where(it => it != null);
        public override IEnumerable<INode> OwnedNodes => localNodes.Concat(Next).Where(it => it != null);
        public ExecutionFlow Flow => this.IsElse ? ExecutionFlow.CreateElse(Body, Next) : ExecutionFlow.CreateFork(Condition, Body, Next);
        // public ExecutionFlow Flow => ExecutionFlow.CreateFork(branches);

        public bool IsComputed => this.Evaluation != null;
        public bool IsDereferenced { get; set; }
        public IEntityInstance Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        private IfBranch(IExpression condition, IEnumerable<IExpression> body, IfBranch next)
        {
            this.condition = condition;
            this.Body = Block.Create(body.Last().ReadMode, body);
            this.Next = next;

            this.ReadMode = Body.ReadMode;
            this.ReadMode = branches.Any(it => it.IsElse)
                                ? (branches.Any(it => it.ReadMode == ExpressionReadMode.ReadRequired) ? ExpressionReadMode.ReadRequired : ExpressionReadMode.OptionalUse)
                                : ExpressionReadMode.CannotBeRead;

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
        }
        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (ReadMode == ExpressionReadMode.CannotBeRead)
                    this.Evaluation = ctx.Env.VoidType.InstanceOf;
                else
                {
                    IEntityInstance eval = this.Body.Evaluated(ctx);

                    if (Next != null)
                    {
                        eval = TypeMatcher.LowestCommonAncestor(ctx, eval, Next.Evaluated(ctx));

                        foreach (IEvaluable part in new IEvaluable[] { Body, Next })
                        {
                            if (part.Evaluation.MatchesTarget(ctx, eval, allowSlicing: false) == TypeMatch.No)
                            {
                                eval = ctx.Env.VoidType.InstanceOf;
                                break;
                            }
                        }
                    }

                    this.Evaluation = eval;
                    this.DataTransfer(ctx, ref this.condition, ctx.Env.BoolType.InstanceOf);

                }

                if (Next != null && IsElse)
                    ctx.ErrorManager.AddError(ErrorCode.MiddleElseBranch, this);
            }
        }

        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
    }
}
