using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityScopeExtension
    {
        public static void Validate(IEntityScope @this, ComputationContext ctx)
        {
            foreach (IMember member in @this.NestedMembers())
                if (!member.Modifier.HasNative
                    && member.Modifier.HasPrivate
                    && !member.Modifier.IsPolymorphic  // in NVI pattern private is used externally
                    && !member.IsMemberUsed)
                {
                    // at this level property accessors are not used, only property itself is used
                    if (member is FunctionDefinition func && func.IsPropertyAccessor(out Property prop) && !prop.IsIndexer)
                        continue;
                    else
                        ctx.AddError(ErrorCode.BindableNotUsed, member.Name);
                }
        }
        public static IEnumerable<IEntity> NestedEntities(this IEntityScope scope)
        {
            return scope.ChildrenNodes.WhereType<IEntity>();
        }

        public static IEnumerable<EntityInstance> NestedEntityInstances(this IEntityScope scope)
        {
            return scope.ChildrenNodes.WhereType<IEntity>().Select(it => it.InstanceOf);
        }
        public static IEnumerable<IEntity> NestedMembers(this IEntityScope scope)
        {
            return scope.ChildrenNodes.WhereType<IMember>();
        }

        public static IEnumerable<EntityInstance> FindEntities(this EntityInstance scopeInstance, ComputationContext ctx,
            NameReference name)
        {
            IEntityScope scope = scopeInstance.Target.Cast<IEntityScope>();

            return scope.AvailableEntities.Find(name)
                .Concat(findTraitEntities(ctx, scopeInstance, name));

        }

        public static IEnumerable<EntityInstance> FindExtensions(this EntityInstance extInstance, ComputationContext ctx,
            NameReference name)
        {
            var available = new HashSet<EntityInstance>(EntityInstance.Comparer);
            IOwnedNode ns = name;
            while (true)
            {
                ns = ns.EnclosingScope<Namespace>();
                if (ns == null)
                    break;

                foreach (Extension ext in ns.NestedExtensions())
                {
                    available.AddRange(findExtensionFunctions(ctx, ext, extInstance));
                }
            }

            foreach (Extension ext in name.EnclosingScopesToRoot().WhereType<TemplateDefinition>().SelectMany(it => it.Includes)
                .Select(it => it.Binding.Match.Instance.Target)
                .WhereType<Extension>())
            {
                available.AddRange(findExtensionFunctions(ctx, ext, extInstance));
            }

            return findEntities(name, available);
        }

        private static IEnumerable<EntityInstance> findExtensionFunctions(ComputationContext ctx, Extension ext, EntityInstance extInstance)
        {
            foreach (EntityInstance func_instance in ext.NestedEntityInstances())
            {
                FunctionDefinition func = func_instance.TargetFunction;
                if (func.IsExtension)
                    yield return func_instance;
            }
        }

        public static IEnumerable<EntityInstance> findEntities(NameReference name, IEnumerable<EntityInstance> entities)
        {
            var result = new List<EntityInstance>();

            foreach (EntityInstance entity_instance in entities)
            {
                IEntity entity = entity_instance.Target;

                if (name.Arity > 0 || entity is TypeContainerDefinition)
                {
                    if (EntityNameArityComparer.Instance.Equals(name, entity.Name))
                        result.Add(entity_instance);
                }
                else
                {
                    if (EntityBareNameComparer.Instance.Equals(name, entity.Name))
                        result.Add(entity_instance.BuildNoArguments());
                }
            }

            return result;
        }

        private static IEnumerable<EntityInstance> findTraitEntities(ComputationContext ctx, EntityInstance scopeInstance,
            NameReference name)
        {
            if (!(scopeInstance.Target is TypeDefinition typedef))
                return Enumerable.Empty<EntityInstance>();

            var result = new List<EntityInstance>();

            foreach (KeyValuePair<TypeDefinition, ScopeTable> entry in typedef.TraitAssociatedEntities)
            {
                IEnumerable<EntityInstance> entities = entry.Value.Find(name);
                if (!entities.Any())
                    continue;

                ConstraintMatch match = TypeMatcher.ArgumentsMatchConstraintsOf(ctx, entry.Key.Name.Parameters, scopeInstance);
                if (match != ConstraintMatch.Yes)
                    continue;

                result.AddRange(entities);
            }

            return result;
        }
    }
}
