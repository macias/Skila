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
    // do NOT use directly -- use Cast from ExpressionFactory

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class ReinterpretType : Expression
    {
        public static ReinterpretType Create(IExpression lhs, INameReference rhsTypeName)
        {
            return new ReinterpretType(lhs, rhsTypeName);
        }

        public IExpression Lhs { get; }
        public INameReference RhsTypeName { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Lhs, RhsTypeName }.Where(it => it != null);
        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private ReinterpretType(IExpression lhs, INameReference rhsTypeName)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Lhs = lhs;
            this.RhsTypeName = rhsTypeName;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreatePath(Lhs));
        }
        public override string ToString()
        {
            string result = $"({RhsTypeName}){Lhs}";
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                this.Evaluation = this.RhsTypeName.Evaluation;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            // we can do whatever but we cannot shake off the const/neutral off
            TypeMutability lhs_mutability = this.Lhs.Evaluation.Components.MutabilityOfType(ctx);
            TypeMutability rhs_mutability = this.RhsTypeName.Evaluation.Components.MutabilityOfType(ctx);
            if (!TypeMatcher.MutabilityMatches(ctx.Env.Options, lhs_mutability, rhs_mutability))
                ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);
        }
    }
}
