using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Printout;
using Skila.Language.Tools;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Undef : Expression
    {
        public static Undef Create()
        {
            return new Undef();
        }

        public override IEnumerable<INode> ChildrenNodes => Enumerable.Empty<INode>();

        // todo: change that mode, undef should only be used on variable initialization
        private Undef() : base(ExpressionReadMode.ReadRequired)
        {
            this.Evaluation = Environment.JokerEval;

            this.attachPostConstructor();
        }

        public override bool IsReadingValueOfNode(IExpression node)
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

        public override string ToString()
        {
            return this.Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeText("undef");
        }
    }
}
