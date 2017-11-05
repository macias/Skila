using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class AddressOf : Expression
    {
        private enum Mode
        {
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

        public override IEnumerable<INode> OwnedNodes => new INode[] { Expr,typename }.Where(it => it != null);

        private NameReference typename;
        private readonly Mode mode;

        private AddressOf(Mode mode, IExpression expr)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Expr = expr;
            this.mode = mode;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"&{Expr}";
            return result;
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.mode == Mode.Reference)
                    typename = NameFactory.ReferenceTypeReference(Expr.Evaluated(ctx).NameOf);
                else
                    typename = NameFactory.PointerTypeReference(Expr.Evaluated(ctx).NameOf);

                typename.AttachTo(this);

                this.Evaluation = typename.Evaluated(ctx);

                Expr.ValidateValueExpression(ctx);

                if (this.mode == Mode.Pointer && !this.Expr.IsLValue(ctx))
                    ctx.AddError(ErrorCode.AddressingRValue, this.Expr);
            }
        }
    }
}
