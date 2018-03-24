using System.Collections.Generic;

namespace Skila.Language.Data
{
    public static partial class LayerDictionary
    {
        public static ILayerDictionary<K,V> Create<K,V>(bool shadowing, IEqualityComparer<K> comparer = null)
            where K:class
            where V:class
        {
            if (shadowing)
                return new StackedLayerDictionary<K, V>(comparer);
            else
                return new FlatLayerDictionary<K, V>(comparer);
        }
    }
}