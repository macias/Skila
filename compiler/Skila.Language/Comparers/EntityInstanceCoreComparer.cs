using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Skila.Language.Comparers
{
    public sealed class EntityInstanceCoreComparer : IEqualityComparer<IEntityInstance>
    {
        public static IEqualityComparer<IEntityInstance> Instance = new EntityInstanceCoreComparer();

        private EntityInstanceCoreComparer()
        {

        }
        public bool Equals(IEntityInstance x, IEntityInstance y)
        {
            return x.CoreEquals(y);
        }

        public int GetHashCode(IEntityInstance obj)
        {
            return obj.Enumerate().Select(it => it.Core).Aggregate(0, (acc, a) => acc ^ RuntimeHelpers.GetHashCode(a));
        }
    }
}
