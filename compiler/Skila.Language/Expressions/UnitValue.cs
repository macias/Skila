using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class UnitValue : Expression
    {
        public static UnitValue Create()
        {
            return new UnitValue();
        }

        private readonly NameReference typeName;
        public override IEnumerable<INode> OwnedNodes => Enumerable.Empty<INode>();

        private UnitValue()
            : base(ExpressionReadMode.ReadRequired)
        {
            this.typeName = NameFactory.UnitTypeReference();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }


        public override bool IsReadingValueOfNode( IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.typeName.Evaluation;
            }
        }
    }
}
