using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class Literal : Expression
    {
        private readonly NameReference typeName;
        public string InputValue { get; }
        public override IEnumerable<INode> OwnedNodes => new[] { typeName };
        public override ExecutionFlow Flow => ExecutionFlow.Empty;

        protected Literal(string inputValue, NameReference typeName)
        : base(ExpressionReadMode.ReadRequired)
        {
            this.typeName = typeName;
            this.InputValue = inputValue;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return this.InputValue;
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
