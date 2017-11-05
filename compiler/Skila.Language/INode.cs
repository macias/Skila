using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public interface INode
    {
#if DEBUG
        DebugId DebugId { get; }
#endif

        INode Owner { get; }
        IScope Scope { get; }
        IEnumerable<INode> OwnedNodes { get; }

        void AttachTo(INode owner);
        void DetachFrom(INode owner);
    }

    public static class INodeExtensions
    {
        public static void AttachNode<T>(this INode owner, ref T placement,T attached)
            where T : INode
        {
            placement = attached;
            placement.AttachTo(owner);
        }
        public static IEnumerable<IScope> EnclosingScopesToRoot(this INode node)
        {
            while (true)
            {
                node = node.Scope;
                if (node == null)
                    break;
                yield return node.Cast<IScope>();
            }
        }
        public static IEnumerable<IScope> ScopesFromSelfToRoot(this INode node)
        {
            {
                if (node is IScope scope)
                    yield return scope;
            }

            foreach (IScope scope in node.EnclosingScopesToRoot())
                yield return scope;

        }
        public static S EnclosingScope<S>(this INode @this)
            where S : IScope
        {
            return @this.EnclosingScopesToRoot().WhereType<S>().FirstOrDefault();
        }
        public static Namespace RootScope(this INode @this)
        {
            return @this.EnclosingScopesToRoot().LastOrDefault() as Namespace;
        }
        public static bool IsType(this INode @this)
        {
            return @this is TypeDefinition;
        }
        public static TypeDefinition CastType(this INode @this)
        {
            return @this.Cast<TypeDefinition>();
        }
        public static Namespace CastNamespace(this INode @this)
        {
            return @this.Cast<Namespace>();
        }
        public static FunctionDefinition CastFunction(this INode @this)
        {
            return @this.Cast<FunctionDefinition>();
        }
        public static bool IsFunction(this INode @this)
        {
            return @this is FunctionDefinition;
        }
        public static bool IsNamespace(this INode @this)
        {
            return @this is Namespace;
        }
        public static bool IsTypeContainer(this INode @this)
        {
            return @this is TypeContainerDefinition;
        }
    }
}
