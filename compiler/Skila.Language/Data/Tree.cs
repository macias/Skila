using System;

namespace Skila.Language.Data
{
    public sealed class Tree<T>
    {
        private readonly Tree<T> parent;
        public T Value { get; }

        public Tree(Tree<T> parent,T value)
        {
            this.parent = parent;
            this.Value = value;
        }

        internal Tree<T> Add(T value)
        {
            var node = new Tree<T>(this, value);
            return node;
        }
    }
}