using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using Skila.Language.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public sealed class Lifetime
    {
        private enum Mode
        {
            None,
            Value,
            Pointer,
            Reference
        }

        // values and pointers are timeless
        public static Lifetime Timeless { get; } = new Lifetime(null, Mode.None, LifetimeScope.Local);

        public static Lifetime Create(INode node, LifetimeScope lifetimeScope = LifetimeScope.Local)
        {
            if (node == null)
                throw new ArgumentNullException();

            return new Lifetime(node, Mode.None,lifetimeScope);
        }

        public static Lifetime CreateReference(INode node)
        {
            if (node == null)
                throw new ArgumentNullException();

            return new Lifetime(node, Mode.Reference, LifetimeScope.Local);
        }
        public static Lifetime CreatePointer(INode node)
        {
            if (node == null)
                throw new ArgumentNullException();

            return new Lifetime(node, Mode.Pointer, LifetimeScope.Local);
        }
        public static Lifetime CreateValue(INode node)
        {
            if (node == null)
                throw new ArgumentNullException();

            return new Lifetime(node, Mode.Value, LifetimeScope.Local);
        }

        private IEnumerable<IScope> __nodeScopes => this.__node.InclusiveScopesToRoot().StoreReadOnly();
        public INode __node { get; }

        private readonly Mode mode;
        private readonly LifetimeScope lifetimeScope;

        public bool IsTimeless => this.__node == null;

        private Lifetime(INode context,Mode mode,LifetimeScope lifetimeScope)
        {
            if (context != null && context.DebugId == (5, 11419))
            {
                ;
            }
            this.__node = context;
            this.mode = mode;
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

            return this.__node == other.__node && this.mode==other.mode;
        }

        public override int GetHashCode()
        {
            return (this.__node?.GetHashCode() ?? 0) ^ this.mode.GetHashCode();
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
            if (this.IsTimeless || source.IsTimeless)
                return false;

            var this_scope = this.__node.EnclosingNode<IEntityScope>();
            var source_func = source.__node.EnclosingNode<FunctionDefinition>();

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
                if (this_scope.DebugId == (35, 320))
                {
                    ;
                }
                return false;
            }

           var source_scope = source.lifetimeScope == LifetimeScope.Attachment ? source.__node.EnclosingScope<TypeDefinition>()
                : source.__node.EnclosingScope<IScope>();

            bool result = this.__node.IsOutOfScopeOf(source_scope);
            if (result)
            {
                ;
            }

            return result;
        }


        internal Lifetime Shorter(Lifetime other)
        {
            if (this.IsTimeless)
                return other;
            else if (other.IsTimeless)
                return this;
            else
                return this.Outlives(other) ? other : this;
        }

        public override string ToString()
        {
            return IsTimeless ? "~~" : this.__node.GetType().Name + "\\" + this.__node.DebugId;
        }
    }
}