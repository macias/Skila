using Skila.Language.Entities;
using System;

namespace Skila.Language
{
    public interface INameRegistry
    {
        void AddLayer(IScope scope);
    }

    public static class INameRegistryExtension
    {
        public static void EnterNode<T>(INode node, ref T registry, Func< T> factory)
           where T : class, INameRegistry
        {
            // if we hit a scope that has a notion of flow (function, block) and there is no name registry set
            // then create it
            if (node is TypeContainerDefinition)
                registry = null;
            else if (node is FunctionDefinition func)
            {
                if (func.IsLambda)
                {
                    ;
                }
                else if (func.IsDeclaration)
                    registry = null;
                else
                {
                    registry = factory();
                }
            }
            else if (registry == null)
            {
                if (node is IExecutableScope)
                    registry = factory();
            }

            registry?.AddLayer(node as IScope);
        }
    }
}