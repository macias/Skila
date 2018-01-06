﻿using NaiveLanguageTools.Common;
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
                    ctx.AddError(ErrorCode.BindableNotUsed, member);
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
            IEnumerable<EntityInstance> entities = scope.AvailableEntities ?? scope.NestedEntityInstances();

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
    }
}
