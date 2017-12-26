using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeInheritance
    {
        // please note Object type does not have Object (itself) in its parents nor in its ancestors
        // ancestors are ordered by their distance from the given type (from immediate parents to Object)
        public IReadOnlyCollection<TypeAncestor> TypeAncestorsWithoutObject { get; }
        public IEnumerable<EntityInstance> AncestorsWithoutObject => this.TypeAncestorsWithoutObject.Select(it => it.AncestorInstance);
        public IEnumerable<TypeAncestor> TypeAncestorsIncludingObject
            => this.addObject ? this.TypeAncestorsWithoutObject.Concat(this.objectType) : this.TypeAncestorsWithoutObject;
        public IEnumerable<EntityInstance> AncestorsIncludingObject => this.TypeAncestorsIncludingObject.Select(it => it.AncestorInstance);

        public IEnumerable<EntityInstance> MinimalParentsWithoutObject { get; }
        public IEnumerable<EntityInstance> MinimalParentsWithObject 
            => this.addObject ? this.MinimalParentsWithoutObject.Concat(this.objectType.AncestorInstance) : this.MinimalParentsWithoutObject;

        private TypeAncestor objectType { get; }

        private readonly bool addObject;

        public TypeInheritance(TypeAncestor objectAncestor, IEnumerable<EntityInstance> minimalParents,
            IEnumerable<TypeAncestor> orderedCompleteAncestors)  // with object, if appropriate
        {
            addObject = orderedCompleteAncestors.Any();

            this.objectType = objectAncestor;
            this.TypeAncestorsWithoutObject = orderedCompleteAncestors
                .Where(it => it.AncestorInstance != objectAncestor.AncestorInstance && !it.AncestorInstance.IsJoker)
                .StoreReadOnly();
            this.MinimalParentsWithoutObject = minimalParents
                .Where(it => it != objectAncestor.AncestorInstance && !it.IsJoker)
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

        public TypeInheritance TranslateThrough(EntityInstance context)
        {
            return new TypeInheritance(this.objectType,
                 this.MinimalParentsWithoutObject.Select(it => it.TranslateThrough(context)),
                 this.TypeAncestorsIncludingObject.Select(it => new TypeAncestor(it.AncestorInstance.TranslateThrough(context),it.Distance)));
        }

        public override string ToString()
        {
            return this.TypeAncestorsWithoutObject.Select(it => it.ToString()).Join(",");
        }

        public EntityInstance GetTypeImplementationParent()
        {
            return this.MinimalParentsWithoutObject.FirstOrDefault(it => it.IsTypeImplementation);
        }
    }
}
