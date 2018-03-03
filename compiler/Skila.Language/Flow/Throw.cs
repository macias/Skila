using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using Skila.Language.Extensions;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Throw : Expression, IFunctionExit
    {
        public static Throw Create(IExpression value = null)
        {
            return new Throw(value);
        }

        private IExpression expr;
        public IExpression Expr => this.expr;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Expr }.Where(it => it != null);

        private Throw(IExpression value) : base(ExpressionReadMode.CannotBeRead)
        {
            this.expr = value;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = "throw";
            if (Expr != null)
                result += " " + Expr.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return node == this.Expr;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.UnitEvaluation;

                NameReference req_typename = NameFactory.PointerTypeReference(NameFactory.ExceptionTypeReference());
                IEntityInstance eval_typename = req_typename.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

                this.DataTransfer(ctx, ref this.expr, eval_typename);
            }
        }
    }
}
