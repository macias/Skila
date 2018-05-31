using System;
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
        internal static Alloc Create(NameReference innerTypename, Memory memory,TypeMutability mutability = TypeMutability.None)
        {
            return new Alloc(innerTypename, memory,mutability);
        }

        private readonly NameReference outcomeTypeName;
        public NameReference InnerTypeName { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { outcomeTypeName }.Where(it => it != null);

        public bool UseHeap { get; }

        private Alloc(NameReference innerTypename,Memory memory, TypeMutability mutability)
            : base(ExpressionReadMode.ReadRequired)
        {
            if (memory == Memory.Stack && mutability != TypeMutability.None)
                throw new ArgumentException();

            this.UseHeap = memory== Memory.Heap;
            this.InnerTypeName = innerTypename;
            this.outcomeTypeName = memory== Memory.Heap? NameFactory.PointerTypeReference(innerTypename,mutability) : innerTypename;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string result = $"alloc<{(UseHeap?"*":"")}{this.InnerTypeName}>";
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
