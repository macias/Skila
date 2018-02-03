using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    // do NOT use directly -- use Cast from ExpressionFactory

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Spread : Expression
    {
        public static Spread Create(IExpression expr)
        {
            return new Spread(expr);
        }

        private IExpression expr;
        public IExpression Expr => this.expr;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Expr }.Where(it => it != null);
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(Expr);

        private bool prepared;

        private Spread(IExpression expr)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.expr = expr;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"...{Expr}";
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                this.Evaluation = this.Expr.Evaluation;
        }

        internal void LiveSetup(ComputationContext ctx, Variadic variadic)
        {
            if (this.prepared)
                throw new Exception("Something wrong?");

            if (!variadic.HasUpperLimit && !variadic.HasLowerLimit)
                this.prepared = true;
            else
            {
                RouteSetup(variadic.MinLimit, variadic.HasUpperLimit ? variadic.MaxLimit : (int?)null);
                this.expr.Evaluated(ctx);
            }
        }

        internal void RouteSetup(int minLimit, int? maxLimit)
        {
            if (this.prepared)
                throw new Exception("Something wrong?");

            this.prepared = true;

            this.expr.DetachFrom(this);
            if (maxLimit.HasValue)
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(IntLiteral.Create($"{minLimit}")),
                    FunctionArgument.Create(IntLiteral.Create($"{maxLimit}")));
            else
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(IntLiteral.Create($"{minLimit}")));

            this.expr.AttachTo(this);
        }
    }
}
