using Skila.Language.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Skila.Language.Comparers
{
    public sealed class EntityInstanceCoreComparer : IEqualityComparer<EntityInstance>
    {
        public static EntityInstanceCoreComparer Instance = new EntityInstanceCoreComparer();

        private EntityInstanceCoreComparer()
        {

        }

        public bool Equals(EntityInstance x, EntityInstance y)
        {
            if (x.Target != y.Target)
                return false;

            if (x.TemplateArguments.Count != y.TemplateArguments.Count)
                return false;

            foreach (var ab in x.TemplateArguments.Zip(y.TemplateArguments, (a, b) => Tuple.Create(a, b)))
                if (!ab.Item1.IsExactlySame(ab.Item2, jokerMatchesAll: false))
                    return false;

            if (!Object.Equals(x.Translation, y.Translation))
                return false;

            return true;
        }

        public int GetHashCode(EntityInstance obj)
        {
            int t = obj.Target.GetHashCode();
            int b = (obj.Translation?.GetHashCode() ?? 0);
            int c = obj.TemplateArguments.Count.GetHashCode();
            int d = obj.TemplateArguments.Aggregate(0, (acc, a) => acc ^ a.GetHashCode());
            return t ^ b ^ c ^ d;
        }
    }
}
