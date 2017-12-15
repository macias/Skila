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
                    && !member.Modifier.IsVirtual  // in NVI pattern private is used externally
                    && !member.IsMemberUsed)
                    ctx.AddError(ErrorCode.BindableNotUsed, member);
        }
        public static IEnumerable<IEntity> NestedEntities(this IEntityScope scope)
        {
            return scope.OwnedNodes.WhereType<IEntity>();
        }
        public static IEnumerable<IEntity> NestedMembers(this IEntityScope scope)
        {
            return scope.OwnedNodes.WhereType<IMember>();
        }

        public static IEnumerable<IEntity> FindEntities(this IEntityScope scope, NameReference name, EntityFindMode findMode)
        {
            IEnumerable<IEntity> entities = scope.AvailableEntities ?? scope.NestedEntities();
            if (scope is TypeDefinition typedef)
            {
                if (findMode == EntityFindMode.WithCurrentProperty)
                {
                    // we need to extend entities if we are inside property, so in getter/setter
                    // we can write "this.prop_field" and get the internal field for property
                    // while outside this property that field is unreachable
                    Property enclosing_property = name.EnclosingScope<Property>();
                    if (enclosing_property != null && enclosing_property.EnclosingScopesToRoot().Contains(typedef))
                        entities = entities.Concat(enclosing_property.AvailableEntities);
                }
                else if (findMode == EntityFindMode.AvailableIndexersOnly)
                {
                    entities = entities.WhereType<Property>(it => it.IsIndexer).SelectMany(prop => prop.AvailableEntities);
                }
            }

            foreach (IEntity entity in entities)
            {
                if (name.Arity > 0)
                {
                    if (EntityNameArityComparer.Instance.Equals(name, entity.Name))
                        yield return entity;
                }
                // coalesce to true, so if we don't have template at all (like simple variable def) then use bare comparison too
                else if ((entity as TemplateDefinition)?.IsFunction() ?? true)
                {
                    if (EntityBareNameComparer.Instance.Equals(name, entity.Name))
                        yield return entity;
                }
                else
                {
                    if (EntityNameArityComparer.Instance.Equals(name, entity.Name))
                        yield return entity;
                }
            }
        }
    }
}
