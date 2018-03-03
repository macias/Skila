using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

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

        public override IEnumerable<INode> OwnedNodes => new INode[] { Lhs, Rhs }.Where(it => it != null);

        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private BoolOperator(OpMode mode, IExpression lhs, IExpression rhs)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Mode = mode;
            this.lhs = lhs;
            this.rhs = rhs;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

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
            string result = $"{Lhs} {Mode} {Rhs}";
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = new EvaluationInfo(ctx.Env.BoolType.InstanceOf);

                this.DataTransfer(ctx, ref this.lhs, ctx.Env.BoolType.InstanceOf);
                this.DataTransfer(ctx, ref this.rhs, ctx.Env.BoolType.InstanceOf);
            }
        }
    }
}
