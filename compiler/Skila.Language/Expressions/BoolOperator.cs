using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using Skila.Language.Tools;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class BoolOperator : Expression
    {
        // if you are looking for exor use not equal instead (same meaning)
        public enum OpMode
        {
            And,
            Or,
        }

        public static BoolOperator Create(OpMode mode, IExpression lhs, IExpression rhs)
        {
            return new BoolOperator(mode, lhs, rhs);
        }

        public OpMode Mode { get; }

        private IExpression lhs;
        private IExpression rhs;

        public IExpression Lhs => this.lhs;
        public IExpression Rhs => this.rhs;

        public override IEnumerable<INode> ChildrenNodes => new IOwnedNode[] { Lhs, Rhs }.Where(it => it != null);

        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private BoolOperator(OpMode mode, IExpression lhs, IExpression rhs)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Mode = mode;
            this.lhs = lhs;
            this.rhs = rhs;

            this.attachPostConstructor();

            this.flow = new Later<ExecutionFlow>(() =>
            {
                switch (this.Mode)
                {
                    case OpMode.And: return ExecutionFlow.CreateFork(Lhs, Rhs, null);
                    case OpMode.Or: return ExecutionFlow.CreateFork(Lhs, null, Rhs);
                    default: throw new InvalidOperationException();
                }
            });
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan(Lhs).Append($" {Mode} ").Append(Rhs);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = EvaluationInfo.Create(ctx.Env.BoolType.InstanceOf);

                this.DataTransfer(ctx, ref this.lhs, ctx.Env.BoolType.InstanceOf);
                this.DataTransfer(ctx, ref this.rhs, ctx.Env.BoolType.InstanceOf);
            }
        }
    }
}
