using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using Skila.Language.Tools;
using Skila.Language.Printout;
using System;

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
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan("*").Append(Expr);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            // in theory we would like to write legal code like
            // (*local_int_ptr) = 1
            // but the problem is dereference can be seen as omitting setter (property or special functions) logic
            // so for now we disable it, and see when for the first time it will be needed in practice
            // note: swap function does not count since we already have parallel assignments
            //
            // and one more small thing, without dereference there is no ambiguity here
            // ref = value
            // does it mean (*ref) = value or ref = &value
            if (!ctx.Env.Options.AllowDereference)
                throw new InvalidOperationException();

                if (this.Evaluation == null)
            {
                if (!ctx.Env.Dereferenced(Expr.Evaluation.Components, out IEntityInstance inner_comp))
                    ctx.AddError(ErrorCode.DereferencingValue, this.Expr);

                ctx.Env.Dereferenced(Expr.Evaluation.Aggregate, out IEntityInstance inner_aggr);

                this.typename = inner_comp.NameOf;
                this.typename.AttachTo(this);
                this.typename.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

                this.Evaluation = EvaluationInfo.Create(inner_comp, inner_aggr.Cast<EntityInstance>());
            }
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            this.typename.ValidateHeapTypeName(ctx,this);
        }

        public override bool IsLValue(ComputationContext ctx)
        {
            return true;
        }
    }
}
