﻿using System.Collections.Generic;
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
        public IReadOnlyCollection<EntityInstance> AncestorsWithoutObject { get; }
        public IEnumerable<EntityInstance> AncestorsIncludingObject
            => this.addObject ? this.AncestorsWithoutObject.Concat(this.objectType) : this.AncestorsWithoutObject;

        public IEnumerable<EntityInstance> MinimalParentsWithoutObject { get; }
        public IEnumerable<EntityInstance> MinimalParentsWithObject 
            => this.addObject ? this.MinimalParentsWithoutObject.Concat(this.objectType) : this.MinimalParentsWithoutObject;

        private EntityInstance objectType { get; }
        private readonly bool addObject;

        public TypeInheritance(EntityInstance objectType, IEnumerable<EntityInstance> minimalParents,
            IEnumerable<EntityInstance> completeAncestors)  // with object, if appropriate
        {
            addObject = completeAncestors.Any();

            this.objectType = objectType;
            this.AncestorsWithoutObject = completeAncestors.Where(it => it != objectType && !it.IsJoker).StoreReadOnly();
            this.MinimalParentsWithoutObject = minimalParents.Where(it => it != objectType && !it.IsJoker).StoreReadOnly();
        }

        public TypeInheritance TranslateThrough(EntityInstance context)
        {
            return new TypeInheritance(this.objectType,
                 this.MinimalParentsWithoutObject,
                 this.AncestorsIncludingObject.Select(it => it.TranslateThrough(context)));
        }

        public override string ToString()
        {
            return this.AncestorsWithoutObject.Select(it => it.ToString()).Join(",");
        }

        public EntityInstance GetTypeImplementationParent()
        {
            return this.MinimalParentsWithoutObject.FirstOrDefault(it => it.IsTypeImplementation);
        }
    }
}
