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

        internal void Setup(ComputationContext ctx, Variadic variadic)
        {
            if (this.prepared)
                throw new Exception("Something wrong?");

            this.prepared = true;

            if (!variadic.HasUpperLimit && !variadic.HasLowerLimit)
                return;

            this.expr.DetachFrom(this);
            if (variadic.HasUpperLimit)
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(IntLiteral.Create($"{variadic.MinLimit}")),
                    FunctionArgument.Create(IntLiteral.Create($"{variadic.MaxLimit}")));
            else
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(IntLiteral.Create($"{variadic.MinLimit}")));

            this.expr.AttachTo(this);

            this.expr.Evaluated(ctx);
        }
    }
}
