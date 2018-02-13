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
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(Lhs);

        private ReinterpretType(IExpression lhs, INameReference rhsTypeName)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Lhs = lhs;
            this.RhsTypeName = rhsTypeName;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
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

            if (this.DebugId.Id == 7)
            {
                ;
            }

            // we can do whatever but we cannot shake off the const/neutral off
            MutabilityFlag lhs_mutability = this.Lhs.Evaluation.Components.MutabilityOfType(ctx);
            MutabilityFlag rhs_mutability = this.RhsTypeName.Evaluation.Components.MutabilityOfType(ctx);
            if (!TypeMatcher.MutabilityMatches(lhs_mutability, rhs_mutability))
                ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);
        }
    }
}
