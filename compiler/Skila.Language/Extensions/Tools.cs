using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language.Extensions
{
    public static class Tools
    {
        public static T Cast<T>(this object @this)
        {
            try
            {
                return (T)@this;
            }
            catch (InvalidCastException ex)
            {
                throw new InvalidCastException("Invalid cast exception for "
                    + (@this == null ? "value null" : "type " + @this.GetType())
                    + " to type " + typeof(T) + ".", ex);
            }
        }

        public static IReadOnlyCollection<T> StoreReadOnly<T>(this IEnumerable<T> coll)
        {
            return coll as IReadOnlyCollection<T> ?? coll.ToArray();
        }


        public static IReadOnlyList<T> StoreReadOnlyList<T>(this IEnumerable<T> coll)
        {
            return coll as IReadOnlyList<T> ?? coll.ToList();
        }
        
        /// <returns>true if at least one elem is less and there is no element greater</returns>
        public static bool HasOneLessNoneGreaterThan<T>(IEnumerable<T> coll, IEnumerable<T> other, Func<T, T, bool> less)
        {
            bool found_less = false;
            foreach (var pair in coll.SyncZip(other))
            {
                if (less(pair.Item2, pair.Item1))
                    return false;
                else if (less(pair.Item1, pair.Item2))
                    found_less = true;
            }

            return found_less;
        }
        /// <summary>
        /// in this algorithm we don't assume transitivity, i.e. having a<b and b<c we doesn't conclude a<c 
        /// </summary>
        public static Option<T> IntransitiveMin<T>(this IEnumerable<T> coll, Func<T, T, bool> less)
        {
            // since we don't have transitivity we have to check the minimum in exhaustive way, i.e. each vs. other comparison
            int count = coll.Count();
            for (int i = 0; i < count; ++i)
            {
                T curr = coll.ElementAt(i);
                bool is_min = true;

                for (int j = 0; is_min && j < count; ++j)
                {
                    if (i != j && !less(curr, coll.ElementAt(j)))
                        is_min = false;
                }

                if (is_min)
                    return new Option<T>(curr);
            }

            return new Option<T>();
        }

    }
}
