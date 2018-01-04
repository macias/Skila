using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Skila.Language
{
    // for each istance of given entity we need translation table despite the fact given entity is not a template at all
    // consider such case -- type Foo<T> with field "foo" of type "T"
    // when we later use type "Foo<Int>" we need to associate translation "T -> Int" to the field
    public sealed class TemplateTranslation
    {
        internal static TemplateTranslation Create(IReadOnlyList<TemplateParameter> parameters, IEnumerable<IEntityInstance> arguments)
        {
            return Combine(null, parameters, arguments);
        }

        private readonly IReadOnlyDictionary<TemplateParameter, IEntityInstance> table;

        private TemplateTranslation(Dictionary<TemplateParameter, IEntityInstance> table)
        {
            this.table = table;
        }

        public override string ToString()
        {
            if (table.Count == 0)
                return "No translations";

            const int limit = 3;
            string s = table.Take(limit).Select(it => $"{it.Key} ==> {it.Value}").Join(", ");
            if (table.Count > limit)
                s += ", ...";
            return s;
        }

        internal static TemplateTranslation Combine(TemplateTranslation trans,
            IReadOnlyList<TemplateParameter> parameters, IEnumerable<IEntityInstance> arguments)
        {
            // it is OK not to give arguments at all for parameters but it is NOT ok to give different number than required
            if (parameters.Any() && arguments.Any() && parameters.Count != arguments.Count())
                throw new NotImplementedException();

            if (!arguments.Any())
                return trans;

            Dictionary<TemplateParameter, IEntityInstance> dict;
            if (trans != null)
                dict = trans.table.ToDictionary(it => it.Key, it => it.Value);
            else
                dict = new Dictionary<TemplateParameter, IEntityInstance>();

            foreach (Tuple<TemplateParameter, IEntityInstance> entry in parameters.SyncZip(arguments.Select(it => it)))
            {
                if (dict.ContainsKey(entry.Item1))
                    dict[entry.Item1] = entry.Item2;
                else
                    dict.Add(entry.Item1, entry.Item2);
            }

            return new TemplateTranslation(dict);
        }

        internal static TemplateTranslation Combine(TemplateTranslation trans1, TemplateTranslation trans2)
        {
            if (trans1 == null)
                return trans2;
            else if (trans2 == null)
                return trans1;

            Dictionary<TemplateParameter, IEntityInstance> dict = trans1.table.ToDictionary(it => it.Key, it => it.Value);

            foreach (KeyValuePair<TemplateParameter, IEntityInstance> entry in trans2.table)
            {
                if (dict.ContainsKey(entry.Key))
                    dict[entry.Key] = entry.Value;
                else
                    dict.Add(entry.Key, entry.Value);
            }

            return new TemplateTranslation(dict);
        }



        public override bool Equals(object obj)
        {
            if (obj is TemplateTranslation trans)
                return this.Equals(trans);
            else
                return false;
        }

        public bool Equals(TemplateTranslation obj)
        {
            if (Object.ReferenceEquals(this, obj))
                return true;

            if (obj == null)
                return false;

            //return this.table.Count == obj.table.Count && !this.table.Except(obj.table).Any();
            if (this.table.Count != obj.table.Count)
                return false;

            foreach (KeyValuePair<TemplateParameter, IEntityInstance> entry in this.table)
                if (!obj.table.TryGetValue(entry.Key, out IEntityInstance value))
                    return false;
                else if (!Object.Equals(entry.Value, value))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return this.table.Aggregate(0, (acc, it) => acc ^ RuntimeHelpers.GetHashCode(it.Key) ^ RuntimeHelpers.GetHashCode(it.Value));
        }

        internal bool Translate(TemplateParameter templateParameter, out IEntityInstance instanceArgument)
        {
            return this.table.TryGetValue(templateParameter, out instanceArgument);
        }
    }

}