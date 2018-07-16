using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Data
{
    public static partial class LayerDictionary
    {
        // you can say it is also transactional or layered dictonary
        // if you remove layer/scope all keys added with that scope/layer are also removed
        private class FlatLayerDictionary<K, V> : ILayerDictionary<K, V>
            where K : class
            where V : class
        {
            // V + layer level
            private readonly Dictionary<K, V> dictionary;
            private readonly Stack<Layer<K, V>> layers;
            private readonly IEqualityComparer<K> comparer;

            public IEnumerable<K> Keys => this.dictionary.Keys;

            public int Count => this.dictionary.Count;

            public FlatLayerDictionary(IEqualityComparer<K> comp)
            {
                comparer = comp ?? EqualityComparer<K>.Default;
                dictionary = new Dictionary<K, V>(comparer);
                layers = new Stack<Layer<K, V>>();
            }

            public void PushLayer()
            {
                layers.Push(new Layer<K, V>(comparer));
            }

            public IEnumerable<Tuple<K, V>> PopLayer()
            {
                var result = new List<Tuple<K, V>>();
                foreach (Tuple<K, V> pair in layers.Peek())
                {
                    K key = pair.Item1;
                    V value = pair.Item2;
                    if (key != null)
                    {
                        result.Add(Tuple.Create(key, this.dictionary[key]));
                        this.dictionary.Remove(key);
                    }
                    else
                        result.Add(pair);
                }
                layers.Pop();

                return result;
            }

            public bool Add(K key, V value)
            {
                if (key != null && dictionary.ContainsKey(key))
                    return false;

                Layer<K, V> top_layer = layers.Peek();
                if (!top_layer.Add(key, value))
                    throw new InvalidOperationException();

                if (key != null)
                    dictionary.Add(key, value);
                return true;
            }
            /*public void RemoveLast(K key, V value)
            {
                if (key != null && !dictionary.ContainsKey(key))
                    throw new ArgumentException();

                Layer<K, V> top_layer = layers.Peek();
                if (!top_layer.Remove(key, value))
                    throw new InvalidOperationException();

                if (key != null && !dictionary.Remove(key))
                    throw new ArgumentException();
            }*/

            public bool TryGetValue(K key, out V value)
            {
                if (dictionary.TryGetValue(key, out value))
                {
                    return true;
                }
                else
                {
                    value = default(V);
                    return false;
                }
            }

            public IEnumerator<KeyValuePair<K, V>> GetEnumerator()
            {
                return this.dictionary.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public V this[K key]
            {
                get
                {
                    return this.dictionary[key];
                }
            }
        }
    }
}
