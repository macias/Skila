using System.Collections.Generic;

namespace Skila.Language.Data
{
    public static partial class LayerDictionary
    {
        public static ILayerDictionary<K,V> Create<K,V>(bool shadowing, IEqualityComparer<K> comp = null)
        {
            if (shadowing)
                return new StackedLayerDictionary<K, V>(comp);
            else
                return new FlatLayerDictionary<K, V>(comp);
        }
    }
}