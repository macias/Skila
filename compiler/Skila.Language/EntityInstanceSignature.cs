using System.Collections.Generic;
using System.Diagnostics;
using Skila.Language.Extensions;
using System.Linq;
using System.Runtime.CompilerServices;
using System;

namespace Skila.Language
{
    // this type exists only to avoid creating duplicate EntityInstance, but since it has already 3 fields it is questionable
    // maybe we should go with creating EntityInstance blindly and if it a duplicate, discarding it
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public struct EntityInstanceSignature
    {
        public static readonly EntityInstanceSignature None = new EntityInstanceSignature(null, false,null);

        public IReadOnlyCollection<IEntityInstance> TemplateArguments { get; }
        public bool OverrideMutability { get; }
        public TemplateTranslation Translation { get; }

        public EntityInstanceSignature(IEnumerable<IEntityInstance> arguments, bool overrideMutability,TemplateTranslation translation)
        {
            this.TemplateArguments = (arguments ?? Enumerable.Empty<IEntityInstance>()).StoreReadOnly();
            this.OverrideMutability = overrideMutability;
            this.Translation = translation;
        }

        public override bool Equals(object obj)
        {
            if (obj is EntityInstanceSignature sig)
                return this.Equals(sig);
            else
                return false;
        }

        public bool Equals(EntityInstanceSignature obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;

            // please note that for functions we need to cache fully specified names and bare as well, consider
            // call<T>(i);
            // and
            // call(i); // type parameters are inferred
            if (this.TemplateArguments.Count != obj.TemplateArguments.Count)
                return false;

            if (this.OverrideMutability != obj.OverrideMutability)
                return false;

            foreach (var ab in this.TemplateArguments.Zip(obj.TemplateArguments, (a, b) => Tuple.Create(a, b)))
                if (!ab.Item1.IsSame(ab.Item2, jokerMatchesAll: false))
                    return false;

            if (!Object.Equals(this.Translation, obj.Translation))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return this.OverrideMutability.GetHashCode()
                ^ this.TemplateArguments.Count.GetHashCode()
                ^ (this.Translation?.GetHashCode() ?? 0)
                ^ this.TemplateArguments.Aggregate(0, (acc, a) => acc ^ RuntimeHelpers.GetHashCode(a));
        }
    }
}
