using Skila.Language.Extensions;
using System;
using System.Collections.Generic;

namespace Skila.Language
{
#if DEBUG
    public readonly struct DebugId 
    {
        private static object threadLock = new object();
        private static Dictionary<Type, ValueTuple<int,int>> typedId = new Dictionary<Type, ValueTuple<int, int>>();

        private readonly ValueTuple<int,int> id;

        public DebugId(Type type) 
        {
            this.id = getId(type);
        }

        private static ValueTuple<int,int> getId(Type type)
        {
            lock (threadLock)
            {
                ValueTuple<int, int> value;
                if (typedId.TryGetValue(type, out value))
                {
                    value = (value.Item1,value.Item2+1);
                    typedId[type] = value;
                }
                else
                {
                    value = (typedId.Count, 0);
                    typedId.Add(type, value);
                }

                return value;
            }
        }

        public override string ToString()
        {
            return id.ToString();
        }

        public override int GetHashCode()
        {
            return this.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return this.id.Equals((obj.Cast<DebugId>()).id);
        }

        public static bool operator==(DebugId cmp,ValueTuple<int,int> other)
        {
            return cmp.id.Item1 == other.Item1 && cmp.id.Item2 == other.Item2;
        }
        public static bool operator !=(DebugId cmp, ValueTuple<int, int> other)
        {
            return !(cmp == other);
        }
    }
#endif
}
