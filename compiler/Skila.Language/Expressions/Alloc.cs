﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Printout;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Alloc : Expression
    {
        internal static Alloc Create(NameReference innerTypename, Memory memory, TypeMutability mutability = TypeMutability.None)
        {
            return new Alloc(innerTypename, memory, mutability);
        }

        private readonly NameReference outcomeTypeName;
        public NameReference InnerTypeName { get; }

        public override IEnumerable<INode> ChildrenNodes => new INode[] { outcomeTypeName }.Where(it => it != null);

        public bool UseHeap { get; }

        private Alloc(NameReference innerTypename, Memory memory, TypeMutability mutability)
            : base(ExpressionReadMode.ReadRequired)
        {
            if (memory == Memory.Stack && mutability != TypeMutability.None)
                throw new ArgumentException();

            this.UseHeap = memory == Memory.Heap;
            this.InnerTypeName = innerTypename;
            this.outcomeTypeName = memory == Memory.Heap
                ? NameFactory.PointerNameReference(this.InnerTypeName, mutability)
                : this.InnerTypeName;

            this.attachPostConstructor();
        }

        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan($"alloc<{(UseHeap ? "*" : "")}").Append(this.InnerTypeName).Append(">()");
        }

        public override bool IsReadingValueOfNode(IExpression node)
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
