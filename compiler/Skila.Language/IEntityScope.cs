using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using System.Collections.Generic;

namespace Skila.Language
{
    // scope which can hold unordered entities (i.e. it is not executed like block)
    public interface IEntityScope : IScope
    {
    }

    public static class IEntityScopeExtension
    {
        public static IEnumerable<IEntity> FindEntities(this IEntityScope @this, NameReference name)
        {
            foreach (IEntity entity in @this.OwnedNodes.WhereType<IEntity>(it => it.Name != null))
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
