using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class IsSame : Expression
    {
        public static IsSame Create(string lhs, string rhs)
        {
            return Create(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IsSame Create(IExpression lhs, IExpression rhs)
        {
            return new IsSame(lhs, rhs);
        }

        public IExpression Lhs { get; }
        public IExpression Rhs { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Lhs, Rhs }.Where(it => it != null);
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(Lhs, Rhs);

        private IsSame(IExpression lhs, IExpression rhs)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Lhs = lhs;
            this.Rhs = rhs;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Lhs} is {Rhs}";
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
            }
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            if (!ctx.Env.IsPointerLikeOfType(this.Lhs.Evaluation.Components))
                ctx.AddError(ErrorCode.CannotUseValueExpression, this.Lhs);
            if (!ctx.Env.IsPointerLikeOfType(this.Rhs.Evaluation.Components))
                ctx.AddError(ErrorCode.CannotUseValueExpression, this.Rhs);

            if (!TypeMatcher.InterchangeableTypes(ctx, this.Lhs.Evaluation.Components, this.Rhs.Evaluation.Components))
                ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);
        }
    }
}
