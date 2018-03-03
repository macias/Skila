using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using System.Linq;

namespace Skila.Language
{
    public interface INameRegistry
    {
    }
    public interface ILayeredNameRegistry : INameRegistry
    {
        void AddLayer(IScope scope);
    }

    public enum EnterNodeRegistryAction
    {
        None,
        Nullify,
        CreateNew
    }
    public static class INameRegistryExtension
    {
        public static void EnterNode<T>(INode node, ref T registry, Func< T> factory)
           where T : class, ILayeredNameRegistry
        {
            EnterNodeRegistryAction action = EnterNode(node, registry);
            CreateRegistry(action,ref registry, factory);

            registry?.AddLayer(node as IScope);
        }

        public static void CreateRegistry<T>(EnterNodeRegistryAction action,ref T registry, Func<T> factory) 
            where T : class, INameRegistry
        {
            if (action == EnterNodeRegistryAction.Nullify)
                registry = null;
            else if (action == EnterNodeRegistryAction.CreateNew)
                registry = factory();
            else if (action != EnterNodeRegistryAction.None)
                throw new Exception();
        }

        public static EnterNodeRegistryAction EnterNode<T>(INode node, T registry)
           where T : class, INameRegistry
        {
            // if we hit a scope that has a notion of flow (function, block) and there is no name registry set
            // then create it
            if (node is TypeContainerDefinition)
                return EnterNodeRegistryAction.Nullify;
            else if (node is FunctionDefinition func)
            {
                if (func.IsLambda)
                {
                    ;
                }
                else if (func.IsDeclaration)
                    return EnterNodeRegistryAction.Nullify;
                else
                {
                    return EnterNodeRegistryAction.CreateNew;
                }
            }
            else if (registry == null)
            {
                if (node is IExecutableScope)
                    return EnterNodeRegistryAction.CreateNew;
            }

            return EnterNodeRegistryAction.None;
        }
    }
}