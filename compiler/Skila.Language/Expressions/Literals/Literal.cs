using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;
using Skila.Language.Printout;

namespace Skila.Language.Expressions.Literals
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class Literal : Expression
    {
        private readonly NameReference typeName;
        public string InputValue { get; }
        public override IEnumerable<INode> OwnedNodes => new[] { typeName };
        public override ExecutionFlow Flow => ExecutionFlow.Empty;

        public abstract object LiteralValue { get; }

        protected Literal(string inputValue, NameReference typeName, ExpressionReadMode readMode = ExpressionReadMode.ReadRequired)
        : base(readMode)
        {
            this.typeName = typeName;
            this.InputValue = inputValue;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return this.Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeText(this.InputValue);
        }

        public override bool IsReadingValueOfNode(IExpression node)
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
