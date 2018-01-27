using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
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
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(TemplateTranslation));
#endif

        public static readonly TemplateTranslation Empty = new TemplateTranslation(new Dictionary<TemplateParameter, IEntityInstance>());

        public static TemplateTranslation Create(IEntity entity, IEnumerable<IEntityInstance> arguments = null)
        {
            var dict = new Dictionary<TemplateParameter, IEntityInstance>();
            foreach (NameDefinition namedef in new[] { entity.Name }
                .Concat(entity.EnclosingScopesToRoot()
                .WhereType<TemplateDefinition>()
                .Select(it => it.Name)))
            {
                foreach (TemplateParameter param in namedef.Parameters)
                    dict.Add(param, null);
            }

            if (arguments != null && arguments.Any())
            {
                // it is OK not to give arguments at all for parameters but it is NOT ok to give different number than required
                IReadOnlyList<TemplateParameter> parameters = entity.Name.Parameters;
                if (parameters.Any() && parameters.Count != arguments.Count())
                    throw new NotImplementedException();

                foreach (var pair in parameters.SyncZip(arguments.Select(it => it)))
                {
                    dict[pair.Item1] = pair.Item2;
                }
            }

            if (dict.Count == 0)
                return Empty;
            else
            {
                return new TemplateTranslation(dict);
            }
        }

        private readonly IReadOnlyDictionary<TemplateParameter, IEntityInstance> table;

        private TemplateTranslation(Dictionary<TemplateParameter, IEntityInstance> table)
        {
            this.table = table;
        }

        public override string ToString()
        {
            const int limit = 3;
            string s = table.Take(limit).Select(it => $"{it.Key} ==> {it.Value}").Join(", ");
            if (table.Count > limit)
                s += ", ...";
            return $"[{table.Count}: {s}]";
        }

        internal static TemplateTranslation Combine(TemplateTranslation basic, TemplateTranslation overlay)
        {
            if (basic == null)
                return overlay;
            else if (overlay == null || basic.table.Count == 0)
                return basic;

            bool translated = false;

            Dictionary<TemplateParameter, IEntityInstance> dict = basic.table.ToDictionary(it => it.Key, it => it.Value);
            foreach (KeyValuePair<TemplateParameter, IEntityInstance> entry in basic.table)
            {
                if (entry.Value != null)
                {
                    bool trans = false;
                    dict[entry.Key] = entry.Value.TranslateThrough(ref trans, overlay);

                    if (trans)
                        translated = true;
                }
                else if (overlay.Translate(entry.Key, out IEntityInstance value))
                {
                    dict[entry.Key] = value;
                    translated = true;
                }
            }

            if (translated)
                return new TemplateTranslation(dict);
            else
                return basic;
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
            return this.table.TryGetValue(templateParameter, out instanceArgument) && instanceArgument != null;
        }

    }

}