using System;
using System.Collections.Generic;

namespace Skila.Language.Data
{
    public interface ILayerDictionary<K, V> : IEnumerable<KeyValuePair<K, V>>
    {
        V this[K key] { get; }

        int Count { get; }
        IEnumerable<K> Keys { get; }

        bool Add(K key, V value);
        IEnumerable<IEnumerable<K>> EnumerateLayers();
        IEnumerable<Tuple<K, V>> PopLayer();
        void PushLayer();
        bool TryGetValue(K key, out V value);
    }
}