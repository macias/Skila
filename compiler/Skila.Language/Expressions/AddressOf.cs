using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class AddressOf : Expression
    {
        private enum Mode
        {
            None,
            Pointer,
            Reference
        }

        public static AddressOf CreateReference(IExpression expr)
        {
            return new AddressOf(Mode.Reference, expr);
        }
        public static AddressOf CreatePointer(IExpression expr)
        {
            return new AddressOf(Mode.Pointer, expr);
        }

        public IExpression Expr { get; }

        public override IEnumerable<INode> ChildrenNodes => new IOwnedNode[] { Expr }.Where(it => it != null);

        private Mode mode;

        private AddressOf(Mode mode, IExpression expr)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Expr = expr;
            this.mode = mode;

            this.attachPostConstructor();
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan("&").Append(Expr);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.mode == Mode.Pointer)
                {
                    this.Evaluation = new EvaluationInfo(
                        ctx.Env.PointerType.GetInstance(
                            TypeMutability.None,
                            TemplateTranslation.Create(ctx.Env.PointerType.InstanceOf,  Expr.Evaluation.Components ),
                            lifetime: Lifetime.Timeless),

                        ctx.Env.PointerType.GetInstance(TypeMutability.None,
                            TemplateTranslation.Create(ctx.Env.PointerType.InstanceOf, Expr.Evaluation.Aggregate ),
                            lifetime: Lifetime.Timeless));
                }
                else if (ctx.Env.IsReferenceOfType(Expr.Evaluation.Components))
                {
                    this.Evaluation = Expr.Evaluation;
                    this.mode = Mode.None;
                }
                else
                {
                    this.Evaluation = new EvaluationInfo(
                        ctx.Env.ReferenceType.GetInstance(
                            TypeMutability.None,
                            TemplateTranslation.Create(ctx.Env.ReferenceType.InstanceOf,  Expr.Evaluation.Components),
                            lifetime: this.Expr.Evaluation.Aggregate.Lifetime),

                        ctx.Env.ReferenceType.GetInstance(TypeMutability.None,
                    TemplateTranslation.Create(ctx.Env.ReferenceType.InstanceOf, Expr.Evaluation.Aggregate ),
                    lifetime: this.Expr.Evaluation.Aggregate.Lifetime));
                }

                Expr.ValidateValueExpression(ctx);

                if (this.mode == Mode.Pointer && !this.Expr.IsLValue(ctx))
                    ctx.AddError(ErrorCode.AddressingRValue, this.Expr);
            }
        }
    }
}
