using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Skila.Language.Comparers
{
    public sealed class EntityInstanceCoreSignatureComparer : IEqualityComparer<EntityInstanceCore>
    {
        public static IEqualityComparer<EntityInstanceCore> Instance = new EntityInstanceCoreSignatureComparer();

        private EntityInstanceCoreSignatureComparer()
        {

        }
        public bool Equals(EntityInstanceCore x, EntityInstanceCore y)
        {
            if (x.TemplateArguments.Count != y.TemplateArguments.Count)
                return false;

            if (x.OverrideMutability != y.OverrideMutability)
                return false;

            foreach (var ab in x.TemplateArguments.Zip(y.TemplateArguments, (a, b) => Tuple.Create(a, b)))
                if (!ab.Item1.IsSame(ab.Item2, jokerMatchesAll: false))
                    return false;

            return true;
        }

        public int GetHashCode(EntityInstanceCore obj)
        {
            return obj.OverrideMutability.GetHashCode()
                            ^ obj.TemplateArguments.Count.GetHashCode()
                            ^ obj.TemplateArguments.Aggregate(0, (acc, a) => acc ^ RuntimeHelpers.GetHashCode(a));
        }
    }
}
