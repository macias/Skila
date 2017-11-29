using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;

namespace Skila.Language.Extensions
{
    public static class INodeExtension
    {
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
        public static S EnclosingScope<S>(this INode @this)
            where S : IScope
        {
            return @this.EnclosingScopesToRoot().WhereType<S>().FirstOrDefault();
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
