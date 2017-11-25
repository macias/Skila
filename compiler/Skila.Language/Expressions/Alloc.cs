using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Alloc : Expression
    {
        internal static Alloc Create(NameReference innerTypename, bool useHeap)
        {
            return new Alloc(innerTypename, useHeap);
        }

        private readonly NameReference outcomeTypeName;
        public NameReference InnerTypeName { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { outcomeTypeName }.Where(it => it != null);

        public bool UseHeap { get; }

        private Alloc(NameReference innerTypename,bool useHeap)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.UseHeap = useHeap;
            this.InnerTypeName = innerTypename;
            this.outcomeTypeName = useHeap? NameFactory.PointerTypeReference(innerTypename) : innerTypename;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string result = $"alloc<{this.InnerTypeName}>";
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
                this.Evaluation = outcomeTypeName.Evaluation;
            }
        }
    }
}
