using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using Skila.Language.Tools;

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

        public override IEnumerable<INode> ChildrenNodes => new INode[] { Expr }.Where(it => it != null);

        private Throw(IExpression value) : base(ExpressionReadMode.CannotBeRead)
        {
            this.expr = value;

            this.attachPostConstructor();
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            var code = new CodeSpan("throw");
            if (Expr != null)
                code.Append(" ").Append(Expr.Printout());
            return code;
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

                NameReference req_typename = NameFactory.PointerNameReference(NameFactory.ExceptionNameReference());
                IEntityInstance eval_typename = req_typename.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

                this.DataTransfer(ctx, ref this.expr, eval_typename);
            }
        }
    }
}
