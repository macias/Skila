using NaiveLanguageTools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Tools
{
    // Dictionary which allows nulls 
    public sealed class NullableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
        where TKey : class
    {
        private Option<TValue> nullValue;
        private readonly IDictionary<TKey, TValue> dict;

        public ICollection<TKey> Keys => this.elements().Select(it => it.Key).ToArray();
        public ICollection<TValue> Values => this.elements().Select(it => it.Value).ToArray();
        public int Count => dict.Count + (this.nullValue.HasValue ? 1 : 0);
        public bool IsReadOnly => dict.IsReadOnly;

        public NullableDictionary(IEqualityComparer<TKey> comparer)
        {
            this.dict = new Dictionary<TKey, TValue>(comparer);
        }
        public NullableDictionary()
        {
            this.dict = new Dictionary<TKey, TValue>();
        }
        public NullableDictionary(int capacity)
        {
            this.dict = new Dictionary<TKey, TValue>(capacity);
        }

        public TValue this[TKey key]
        {
            get
            {
                if (key == null)
                {
                    if (!nullValue.HasValue)
                        throw new KeyNotFoundException();
                    return nullValue.Value;
                }
                else
                    return this.dict[key];
            }
            set
            {
                if (key == null)
                    nullValue = new Option<TValue>(value);
                else
                    this.dict[key] = value;
            }
        }

        private IEnumerable<KeyValuePair<TKey, TValue>> elements()
        {
            if (nullValue.HasValue)
                return this.dict.Concat(new KeyValuePair<TKey, TValue>(null, nullValue.Value));
            else
                return this.dict;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.elements().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (key == null)
            {
                value = nullValue.DefValue;
                return this.nullValue.HasValue;
            }
            else
                return this.dict.TryGetValue(key, out value);
        }

        public void Add(TKey key, TValue value)
        {
            if (key == null)
            {
                if (this.nullValue.HasValue)
                    throw new ArgumentException();
                this.nullValue = new Option<TValue>(value);
            }
            else
                this.dict.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            if (key == null)
                return this.nullValue.HasValue;
            else
                return dict.ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            if (key == null)
            {
                bool result = this.nullValue.HasValue;
                this.nullValue = new Option<TValue>();
                return result;
            }
            else
                return dict.Remove(key);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            this.nullValue = new Option<TValue>();
            dict.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key != null)
                return dict.Contains(item);
            else if (!this.nullValue.HasValue)
                return false;
            else
                return EqualityComparer<TValue>.Default.Equals(this.nullValue.Value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (this.nullValue.HasValue)
            {
                array[arrayIndex] = new KeyValuePair<TKey, TValue>(null, this.nullValue.Value);
                ++arrayIndex;
            }
            dict.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (item.Key != null)
                return dict.Remove(item);
            else if (!this.nullValue.HasValue)
                return false;
            else
            {
                bool result = EqualityComparer<TValue>.Default.Equals(this.nullValue.Value, item.Value);
                this.nullValue = new Option<TValue>();
                return result;
            }
        }
    }
}
