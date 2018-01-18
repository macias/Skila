using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Dereference : Expression
    {
        public static Dereference Create(IExpression expr)
        {
            return new Dereference(expr);
        }

        public IExpression Expr { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Expr, typename }.Where(it => it != null);

        private INameReference typename;

        private Dereference(IExpression expr)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Expr = expr;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"*{Expr}";
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
                IEntityInstance inner;
                if (!ctx.Env.Dereferenced(Expr.Evaluation.Components, out inner))
                    ctx.AddError(ErrorCode.DereferencingValue, this.Expr);

                this.typename = inner.NameOf;
                this.typename.AttachTo(this);
                this.typename.Evaluated(ctx);

                this.Evaluation = typename.Evaluation;
            }
        }

        public override bool IsLValue(ComputationContext ctx)
        {
            return true;
        }
    }
}
