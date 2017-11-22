﻿using System;
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
        public static IExpression Readout(string name)
        {
            return ExpressionFactory.Readout(name);
        }
        public static IExpression Readout(IExpression expr)
        {
            return ExpressionFactory.Readout(expr);
        }
        public static IEntity Target(this IEntityInstance instance)
        {
            if (instance is EntityInstance)
                return (instance as EntityInstance).Target;
            else if (instance is EntityInstanceUnion)
                return (instance as EntityInstanceUnion).Instances.Single().Target();
            else
                throw new NotImplementedException();
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
