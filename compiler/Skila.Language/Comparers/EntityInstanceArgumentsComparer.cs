using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Comparers
{
    public sealed class EntityInstanceArgumentsComparer : IEqualityComparer<IReadOnlyList<IEntityInstance>>
    {
        public static EntityInstanceArgumentsComparer Instance = new EntityInstanceArgumentsComparer();

        private EntityInstanceArgumentsComparer()
        {

        }

        public bool Equals(IReadOnlyList<IEntityInstance> x, IReadOnlyList<IEntityInstance> y)
        {
            if (x.Count != y.Count)
                return false;

            for (int i = 0; i < x.Count; ++i)
                if (!x[i].IsExactlySame(y[i], jokerMatchesAll: false))
                    return false;

            return true;
        }

        public int GetHashCode(IReadOnlyList<IEntityInstance> obj)
        {
            int c = obj.Count.GetHashCode();
            int d = obj.Aggregate(0, (acc, a) => acc ^ a.GetHashCode());
            return c ^ d;
        }
    }
}
