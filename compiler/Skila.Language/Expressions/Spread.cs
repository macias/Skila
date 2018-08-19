using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions.Literals;
using Skila.Language.Tools;
using Skila.Language.Printout;

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

        public override IEnumerable<INode> ChildrenNodes => new INode[] { Expr }.Where(it => it != null);
        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private bool prepared;

        private Spread(IExpression expr)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.expr = expr;

            this.attachPostConstructor();

            this.flow = Later.Create(() => ExecutionFlow.CreatePath(Expr));
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan("...").Append(Expr.Printout());
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

            RouteSetup(variadic.MinLimit, variadic.HasUpperLimit ? variadic.Max1Limit : (int?)null);
            this.expr.Evaluated(ctx, EvaluationCall.AdHocCrossJump);
        }

        internal void RouteSetup(int? minLimit, int? max1Limit)
        {
            if (this.prepared)
                throw new Exception("Something wrong?");

            this.prepared = true;

            this.expr.DetachFrom(this);
            if (max1Limit.HasValue)
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(NatLiteral.Create($"{minLimit}")),
                    FunctionArgument.Create(NatLiteral.Create($"{max1Limit}")));
            else
                this.expr = FunctionCall.Create(NameFactory.SpreadFunctionReference(), FunctionArgument.Create(this.expr),
                    FunctionArgument.Create(NatLiteral.Create($"{minLimit}")));

            this.expr.AttachTo(this);
        }
    }
}
