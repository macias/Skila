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
        private class StackedLayerDictionary<K, V> : ILayerDictionary<K, V>
        {
            // V + layer level
            private readonly Dictionary<K, Stack<V>> dictionary;
            private readonly Stack<HashSet<K>> layers;
            private readonly IEqualityComparer<K> comparer;

            public IEnumerable<K> Keys => this.dictionary.Keys;

            public int Count => this.dictionary.Count;

            public StackedLayerDictionary(IEqualityComparer<K> comp)
            {
                comparer = comp ?? EqualityComparer<K>.Default;
                dictionary = new Dictionary<K, Stack<V>>(comparer);
                layers = new Stack<HashSet<K>>();
            }

            public void PushLayer()
            {
                layers.Push(new HashSet<K>(comparer));
            }

            /*  private V pop(List<V> list)
              {
                  V last = list[list.Count - 1];
                  list.RemoveAt(list.Count - 1);
                  return last;
              }*/

            public IEnumerable<Tuple<K, V>> PopLayer()
            {
                var result = new List<Tuple<K, V>>();
                foreach (K key in layers.Peek())
                {
                    Stack<V> stack = this.dictionary[key];
                    result.Add(Tuple.Create(key, stack.Pop()));
                    if (!stack.Any())
                        this.dictionary.Remove(key);
                }
                layers.Pop();

                return result;
            }

            /*internal IEnumerable<V> GetAllValues()
            {
                return this.dictionary.Values.Select(it => it.Item1);
            }*/

            /*internal IEnumerable<K> GetLastScopeKeys()
            {
                if (layers.Count==0)
                    return Enumerable.Empty<K>();
                else
                    return layers.Last.Value.Item2;
            }*/

            public bool Add(K key, V value)
            {
                Stack<V> values;
                if (!dictionary.TryGetValue(key, out values))
                {
                    values = new Stack<V>();
                    this.dictionary.Add(key, values);
                }

                if (!layers.Peek().Add(key))
                    throw new InvalidOperationException();
                values.Push(value);
                return true;
            }

            /*public void RemoveLast(K key, V value)
            {
                Stack<V> values;
                if (!dictionary.TryGetValue(key, out values))
                    throw new ArgumentException();

                if (!layers.Peek().Remove(key))
                    throw new ArgumentException();

                V last = values.Pop();
                if (!Object.Equals(last, value))
                    throw new ArgumentException();
            }*/

            /*internal bool Remove(K key)
    {
       Tuple<V, int> tuple;
       if (!dictionary.TryGetValue(key, out tuple))
           return false;

       if (!layers.ElementAt(tuple.Item2).Item2.Remove(key))
           throw new InvalidOperationException();
       dictionary.Remove(key);
       return true;
    }*/

            public bool TryGetValue(K key, out V value)
            {
                if (dictionary.TryGetValue(key, out Stack<V> values))
                {
                    value = values.Peek();
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
                foreach (KeyValuePair<K, Stack<V>> entry in this.dictionary)
                    yield return new KeyValuePair<K, V>(entry.Key, entry.Value.Peek());
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            public V this[K key]
            {
                get
                {
                    return this.dictionary[key].Peek();
                }
            }
        }
    }
}
