using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;

namespace Skila.Language.Extensions
{
    public static class INodeExtension
    {
        public static IEnumerable<TypeDefinition> NestedTypes(this INode node)
        {
            return node.ChildrenNodes.WhereType<TypeDefinition>();
        }

        public static IEnumerable<Extension> NestedExtensions(this INode node)
        {
            return node.ChildrenNodes.WhereType<Extension>();
        }

        public static IEnumerable<INode> DescendantNodes(this INode node)
        {
            return node.ChildrenNodes.Concat(node.ChildrenNodes.SelectMany(it => it.DescendantNodes()));
        }
        public static IEnumerable<IOwnedNode> EnclosingNodesToRoot(this IOwnedNode node)
        {
            while (true)
            {
                node = node.Owner;
                if (node == null)
                    break;
                yield return node;
            }
        }
        public static IEnumerable<IScope> EnclosingScopesToRoot(this IOwnedNode node)
        {
            while (true)
            {
                node = node.Scope;
                if (node == null)
                    break;
                yield return node.Cast<IScope>();
            }
        }

        public static S EnclosingScope<S>(this IOwnedNode @this)
            where S : IScope
        {
            return @this.EnclosingScopesToRoot().WhereType<S>().FirstOrDefault();
        }
        public static N EnclosingNode<N>(this IOwnedNode @this)
            where N : IOwnedNode
        {
            return @this.EnclosingNodesToRoot().WhereType<N>().FirstOrDefault();
        }
        public static bool IsType(this IOwnedNode @this)
        {
            return @this is TypeDefinition;
        }
        public static TypeDefinition CastType(this IOwnedNode @this)
        {
            return @this.Cast<TypeDefinition>();
        }
        public static TypeContainerDefinition CastTypeContainer(this IOwnedNode @this)
        {
            return @this.Cast<TypeContainerDefinition>();
        }
        public static Namespace CastNamespace(this IOwnedNode @this)
        {
            return @this.Cast<Namespace>();
        }
        public static FunctionDefinition CastFunction(this IOwnedNode @this)
        {
            return @this.Cast<FunctionDefinition>();
        }
        public static bool IsFunction(this IOwnedNode @this)
        {
            return @this is FunctionDefinition;
        }
        public static bool IsNamespace(this IOwnedNode @this)
        {
            return @this is Namespace;
        }
        public static bool IsTypeContainer(this IOwnedNode @this)
        {
            return @this is TypeContainerDefinition;
        }
    }
}
