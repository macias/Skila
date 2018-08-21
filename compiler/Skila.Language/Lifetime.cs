using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public sealed class Lifetime
    {
        // values and pointers are timeless
        public static Lifetime Timeless { get; } = new Lifetime(null, LifetimeScope.Global);

        public static Lifetime Create(IOwnedNode node, LifetimeScope lifetimeScope = LifetimeScope.Local)
        {
            if (node == null)
                throw new ArgumentNullException();

            return new Lifetime(node, lifetimeScope);
        }

        private readonly IOwnedNode node;
        private readonly LifetimeScope lifetimeScope;

        private IEnumerable<IScope> nodeScopes => this.node.EnclosingScopesToRoot();
        public bool IsAttached => this.lifetimeScope == LifetimeScope.Attachment;
        public bool IsTimeless => this.node == null;

        private Lifetime(IOwnedNode context, LifetimeScope lifetimeScope)
        {
            this.node = context;
            this.lifetimeScope = lifetimeScope;
        }

        public override bool Equals(object obj)
        {
            if (this.GetType() != obj?.GetType())
                throw new ArgumentException();
            else
                return Equals((Lifetime)obj);
        }

        public bool Equals(Lifetime other)
        {
            if (ReferenceEquals(this, other))
                return true;
            else if (ReferenceEquals(other, null))
                return false;

            return this.node == other.node && this.lifetimeScope == other.lifetimeScope;
        }

        public override int GetHashCode()
        {
            return (this.node?.GetHashCode() ?? 0) ^ this.lifetimeScope.GetHashCode();
        }

        public static bool operator ==(Lifetime a, Lifetime b)
        {
            return Object.Equals(a, b);
        }

        public static bool operator !=(Lifetime a, Lifetime b)
        {
            return !(a == b);
        }

        public bool Outlives(Lifetime source)
        {
            if (source.lifetimeScope == LifetimeScope.Attachment && this.lifetimeScope != LifetimeScope.Attachment)
                return true;

            if (this.IsTimeless || source.IsTimeless)
                return false;

            var this_scope = this.node.EnclosingNode<IEntityScope>();
            var source_func = source.node.EnclosingNode<FunctionDefinition>();

            if (this_scope is FunctionDefinition && this_scope != source_func)
            {
                // since target lifetime is bound to another function we can safely pass the reference to local data
                // because we know the reference will not be stored (it would be bound to storage type then)
                // example: calling `max(foo,bar)` where `max` takes references to its parameters
                return false;
            }
            else if (this_scope is Property this_prop && !this_prop.Accessors.Contains(source_func))
            {
                // similar rationale as above but we have to exlude accessors, because this would mean
                // we allow setter/getter to store its reference parameter as property field
                return false;
            }

            var source_scope = source.node.EnclosingScope<IScope>();

            bool result = !this.nodeScopes.Contains(source_scope);

            if (result)
            {
                ;
            }

            return result;
        }


        internal Lifetime Shorter(Lifetime other)
        {
            if (other == null || other.IsTimeless)
                return this;
            else if (this.IsTimeless)
                return other;
            else
                return this.Outlives(other) ? other : this;
        }

        public override string ToString()
        {
            return IsTimeless ? "~~" : this.node.GetType().Name + "\\" + this.node.DebugId;
        }

        internal Lifetime AsAttached()
        {
            return new Lifetime(this.node, LifetimeScope.Attachment);
        }
    }
}