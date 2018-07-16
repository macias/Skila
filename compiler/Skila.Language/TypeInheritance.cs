using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Entities;
using System;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeInheritance
    {
        // please note Object type does not have Object (itself) in its parents nor in its ancestors
        // ancestors are ordered by their distance from the given type (from immediate parents to Object)
        public IReadOnlyCollection<TypeAncestor> OrderedTypeAncestorsWithoutObject { get; }
        public IEnumerable<EntityInstance> OrderedAncestorsWithoutObject => this.OrderedTypeAncestorsWithoutObject.Select(it => it.AncestorInstance);
        public IEnumerable<TypeAncestor> OrderedTypeAncestorsIncludingObject
            => this.addObject ? this.OrderedTypeAncestorsWithoutObject.Concat(this.objectType) : this.OrderedTypeAncestorsWithoutObject;
        public IEnumerable<EntityInstance> OrderedAncestorsIncludingObject => this.OrderedTypeAncestorsIncludingObject.Select(it => it.AncestorInstance);

        public IEnumerable<EntityInstance> MinimalParentsWithoutObject { get; }
        public IEnumerable<EntityInstance> MinimalParentsIncludingObject
            => this.addObject ? this.MinimalParentsWithoutObject.Concat(this.objectType.AncestorInstance) : this.MinimalParentsWithoutObject;

        private TypeAncestor objectType { get; }

        private readonly bool addObject;

        public TypeInheritance(TypeAncestor objectAncestor, IEnumerable<EntityInstance> minimalParents,
            IEnumerable<TypeAncestor> completeAncestors)  // with object, if appropriate
        {
            addObject = completeAncestors.Any();

            this.objectType = objectAncestor;
            this.OrderedTypeAncestorsWithoutObject = completeAncestors.OrderBy(it => it.Distance)
                .Where(it => !it.AncestorInstance.IsIdentical( objectAncestor.AncestorInstance) && !it.AncestorInstance.IsJoker)
                .StoreReadOnly();
            this.MinimalParentsWithoutObject = minimalParents
                .Where(it => !it.IsIdentical(objectAncestor.AncestorInstance) && !it.IsJoker)
                .StoreReadOnly();

            /* EntityInstance impl_parent = this.GetTypeImplementationParent();
             if (impl_parent != null)
             {
                 EntityInstance first_parent = this.MinimalParentsWithoutObject.First();
                 EntityInstance first_ancestor = this.AncestorsWithoutObject.Select(it => it.AncestorInstance).First();
                 if (impl_parent != first_parent)
                     throw new Exception("Parent implementation should be the first parent");
                 if (impl_parent != first_ancestor)
                     throw new Exception("Parent implementation should be the first ancestor");
             }*/
        }

        public TypeInheritance TranslateThrough(ComputationContext ctx, EntityInstance closedTemplate)
        {
            IEnumerable<TypeDefinition> traits = closedTemplate.AvailableTraits(ctx);

            IEnumerable<EntityInstance> minimal_parents = this.MinimalParentsWithoutObject
                    .Concat(traits.SelectMany(it => it.Inheritance.MinimalParentsWithoutObject))
                    .Select(it => it.TranslateThrough(closedTemplate))
                    .Distinct(EntityInstance.Comparer);

            var dict = new Dictionary<EntityInstance, int>(EntityInstance.Comparer);
            foreach (TypeAncestor ancestor in this.OrderedTypeAncestorsIncludingObject
                .Concat(traits.SelectMany(it => it.Inheritance.OrderedTypeAncestorsIncludingObject))
                .Select(it => it.TranslateThrough(closedTemplate)))
            {
                if (dict.TryGetValue(ancestor.AncestorInstance, out int dist))
                    dict[ancestor.AncestorInstance] = Math.Min(dist, ancestor.Distance);
                else
                    dict.Add(ancestor.AncestorInstance, ancestor.Distance);
            }

            return new TypeInheritance(this.objectType,
                 minimal_parents,
                 completeAncestors: dict.Select(it => new TypeAncestor(it.Key, it.Value)));
        }

        public override string ToString()
        {
            return this.OrderedTypeAncestorsWithoutObject.Select(it => it.ToString()).Join(",");
        }

        public EntityInstance GetTypeImplementationParent()
        {
            return this.MinimalParentsWithoutObject.FirstOrDefault(it => it.IsTypeImplementation);
        }
    }
}
