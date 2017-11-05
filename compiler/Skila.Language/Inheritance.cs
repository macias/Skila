using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Inheritance
    {
        // please note Object type does not have Object (itself) in its parents nor in its ancestors
        public IReadOnlyCollection<EntityInstance> AncestorsWithoutObject { get; }
        public IEnumerable<EntityInstance> AncestorsIncludingObject => addObject ? this.AncestorsWithoutObject.Concat(this.objectType) : this.AncestorsWithoutObject;

        public IEnumerable<EntityInstance> MinimalParentsWithoutObject { get; }

        private EntityInstance objectType { get; }
        //public bool HasLoop { get; }
        private  readonly bool addObject;

        public Inheritance(EntityInstance objectType,IEnumerable<EntityInstance> minimalParents,
            IEnumerable<EntityInstance> completeAncestors  // with object, if appropriate
            //bool looped
            )
        {
            addObject = !completeAncestors.Any();

            this.objectType = objectType;
            this.AncestorsWithoutObject = completeAncestors.Where(it => it != objectType && !it.IsJoker).StoreReadOnly();
            this.MinimalParentsWithoutObject = minimalParents.Where(it => it != objectType && !it.IsJoker).StoreReadOnly();
            //this.HasLoop = looped;
        }

        public Inheritance TranslateThrough(EntityInstance context)
        {
           return new Inheritance(this.objectType,
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
