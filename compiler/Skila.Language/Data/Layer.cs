using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Data
{
    // currently it is not really used because we don't use anonymous entries, but since it is already written
    // it would be a pity to trash, so for now let it stay

    internal sealed class Layer<K, V> : IEnumerable<Tuple<K, V>>
        where K : class
        where V : class
    {
        private readonly HashSet<K> named;
        private readonly List<V> anonymous;
        private readonly IEqualityComparer<K> comparer;

        public Layer(IEqualityComparer<K> comp)
        {
            this.comparer = comp ?? EqualityComparer<K>.Default;
            this.named = new HashSet<K>(comparer);
            this.anonymous = new List<V>();
        }

        public IEnumerator<Tuple<K, V>> GetEnumerator()
        {
            return this.named.Select(it => Tuple.Create(it, (V)null))
                .Concat(this.anonymous.Select(it => Tuple.Create((K)null, it))).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal bool Add(K key, V value)
        {
            if (key == null)
            {
                this.anonymous.Add(value);
                return true;
            }
            else
                return this.named.Add(key);
        }

        /*internal bool Remove(K key, V value)
        {
            if (key == null)
            {
                return this.anonymous.Remove(value);
            }
            else
                return this.named.Remove(key);
        }*/
    }
}
