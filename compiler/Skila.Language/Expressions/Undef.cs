using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Undef : Expression
    {
        public static Undef Create()
        {
            return new Undef();
        }

        public override IEnumerable<INode> OwnedNodes => Enumerable.Empty<INode>();

        private Undef()
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Evaluation = EntityInstance.Joker;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            /*if (this.Evaluation == null)
            {
                this.Evaluation = this.typeName.Evaluated(ctx);
            }*/
        }
    }
}
