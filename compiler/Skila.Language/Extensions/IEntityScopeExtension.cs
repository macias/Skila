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
            return scope.OwnedNodes.WhereType<IEntity>();
        }

        public static IEnumerable<EntityInstance> NestedEntityInstances(this IEntityScope scope)
        {
            return scope.OwnedNodes.WhereType<IEntity>().Select(it => it.InstanceOf);
        }
        public static IEnumerable<IEntity> NestedMembers(this IEntityScope scope)
        {
            return scope.OwnedNodes.WhereType<IMember>();
        }

        public static IEnumerable<EntityInstance> FindEntities(this IEntityScope scope, NameReference name, EntityFindMode findMode)
        {
            IEnumerable<EntityInstance> entities = filterAvailableEntities(availableEntities(scope), scope, name, findMode);
            return findEntities(name, entities);
        }
        public static IEnumerable<EntityInstance> FindEntities(this EntityInstance scopeInstance, ComputationContext ctx, NameReference name, EntityFindMode findMode)
        {
            IEnumerable<EntityInstance> entities = filterAvailableEntities(availableEntities(ctx, scopeInstance), scopeInstance.TargetTemplate, name, findMode);
            return findEntities(name, entities);
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
                        result.Add(entity_instance);
                }
            }

            return result;
        }

        private static IEnumerable<EntityInstance> availableEntities(IEntityScope scope)
        {
            IEnumerable<EntityInstance> entities = scope.AvailableEntities ?? scope.NestedEntityInstances();

            return entities;
        }


        private static IEnumerable<EntityInstance> availableEntities(ComputationContext ctx, EntityInstance scopeInstance)
        {
            IEntityScope scope = scopeInstance.Target.Cast<TemplateDefinition>();

            IEnumerable<EntityInstance> entities = availableEntities(scope);

            if (scope is TypeDefinition typedef)
            {
                foreach (TypeDefinition trait in scopeInstance.AvailableTraits(ctx))
                {
                    IEnumerable<EntityInstance> trait_entities = trait.AvailableEntities
                        .Select(it => it.TranslateThroughTraitHost(trait: trait))
                        .Where(it => !it.Target.IsAnyConstructor());

                    entities = entities.Concat(trait_entities);
                }
            }

            return entities;
        }

        private static IEnumerable<EntityInstance> filterAvailableEntities(IEnumerable<EntityInstance> entities,
            IEntityScope scope, NameReference name, EntityFindMode findMode)
        {
            if (scope is TypeDefinition typedef)
            {
                if (findMode == EntityFindMode.WithCurrentProperty)
                {
                    // we need to extend entities if we are inside property, so in getter/setter
                    // we can write "this.prop_field" and get the internal field for property
                    // while outside this property that field is unreachable
                    Property enclosing_property = name.EnclosingScope<Property>();
                    if (enclosing_property != null)
                    {
                        EntityInstance prop_instance = entities.SingleOrDefault(it => it.Target == enclosing_property);
                        if (prop_instance != null)
                            entities = entities.Concat(enclosing_property.AvailableEntities
                                .Select(it => it.TranslateThrough(prop_instance)));
                    }
                }
                else if (findMode == EntityFindMode.AvailableIndexersOnly)
                {
                    entities = entities.Where(it => it.Target is Property prop && prop.IsIndexer)
                        .SelectMany(prop => prop.Target.Cast<Property>().AvailableEntities.Select(it => it.TranslateThrough(prop)));
                }
            }

            return entities;
        }
    }
}
