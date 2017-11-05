using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Data
{
    // you can say it is also transactional or layered dictonary
    // if you remove layer/scope all keys added with that scope/layer are also removed
    public class LayerDictionary<K, V> : IEnumerable<KeyValuePair<K,V>>
    {
        // V + layer level
        private readonly Dictionary<K, V> dictionary;
        private readonly Stack<HashSet<K>> layers;
        private readonly IEqualityComparer<K> comparer;

        public IEnumerable<K> Keys => this.dictionary.Keys;

        public int Count => this.dictionary.Count;

        public LayerDictionary(IEqualityComparer<K> comp = null)
        {
            comparer = comp ?? EqualityComparer<K>.Default;
            dictionary = new Dictionary<K,V>(comparer);
            layers = new Stack<HashSet<K>>();
        }

        public IEnumerable<IEnumerable<K>> EnumerateLayers()
        {
            return this.layers.Reverse(); // from first to last
        }
        public void PushLayer()
        {
            layers.Push(new HashSet<K>(comparer));
        }

        /*public L GetLastScope()
        {
            if (scopes.Any())
                return scopes.Last.Value.Item1;
            else
                return null;
        }*/
        public IEnumerable<Tuple<K,V>> PopLayer()
        {
            var result = new List<Tuple<K,V>>();
            foreach (K key in layers.Peek())
            {
                result.Add(Tuple.Create(key,  this.dictionary[key]));
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
            if (dictionary.ContainsKey(key))
                return false;

            if (!layers.Peek().Add(key))
                throw new InvalidOperationException();
            dictionary.Add(key,value);
            return true;
        }
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
