using System;
using System.Linq;
using Skila.Language;
using System.Collections.Generic;

namespace Skila.Tests
{
    public static class Tools
    {
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
                return (nameRef as NameReferenceUnion).Elements.Single().Binding();
            else
                throw new NotImplementedException();
        }
    }
}
