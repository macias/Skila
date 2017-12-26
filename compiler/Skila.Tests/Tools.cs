using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using System.Collections.Generic;
using Skila.Language.Extensions;

namespace Skila.Tests
{
    public static class Tools
    {
        public static IEnumerable<T> GetEnumValues<T>()
            where T : struct
        {
           foreach (object v in Enum.GetValues(typeof(T)))
            {
                yield return v.Cast<T>();
            }
        }
        public static void AddRange<T>(this HashSet<T> @this,IEnumerable<T> values)
        {
            foreach (T v in values)
                @this.Add(v);
        }
      
        public static Binding Binding(this INameReference nameRef)
        {
            if (nameRef is NameReference)
                return (nameRef as NameReference).Binding;
            else if (nameRef is NameReferenceUnion)
                return (nameRef as NameReferenceUnion).Names.Single().Binding();
            else
                throw new NotImplementedException();
        }
    }
}
