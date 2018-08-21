using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static readonly TemplateTranslation Empty = new TemplateTranslation(null, new Dictionary<TemplateParameter, IEntityInstance>());

        public static TemplateTranslation Create(IEntity entity)
        {
            // todo: especially this is pretty inefficient because if we have basic entity at hand
            // we could in theory even create empty translation, however we can be dependent on parent entities
            // thus maybe it would be beneficial to compute those translation tables in top-down approach
            // and if we are in template-free branch we could create empty table faster
            return Create(entity, Enumerable.Empty<IEntityInstance>());
        }
        public static TemplateTranslation Create(IEntity entity, IEnumerable<IEntityInstance> arguments)
        {
            Dictionary<TemplateParameter, IEntityInstance> dict
                = entity.Name.Parameters.ToDictionary(it => it, it => (IEntityInstance)null);

            foreach (TemplateDefinition template in entity.EnclosingScopesToRoot().WhereType<TemplateDefinition>())
            {
                foreach (TemplateParameter param in template.Name.Parameters)
                    dict.Add(param, null);
            }

            IReadOnlyList<TemplateParameter> parameters = entity.Name.Parameters;
            if (arguments != null && arguments.Any())
            {
                // it is OK not to give arguments at all for parameters but it is NOT ok to give different number than required
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
                return new TemplateTranslation(entity, dict);
            }
        }

        private readonly IReadOnlyDictionary<TemplateParameter, IEntityInstance> table;
        private readonly int hashCode;
        private readonly IEntity entity;

        private IReadOnlyList<TemplateParameter> primaryParameters => this.entity?.Name?.Parameters;
        public IReadOnlyList<IEntityInstance> PrimaryArguments { get; }

        private TemplateTranslation(IEntity entity,
            Dictionary<TemplateParameter, IEntityInstance> table)
        {
            this.table = table;
            this.hashCode = this.table.Aggregate(0, (acc, it) => acc ^ it.Key.GetHashCode() ^ (it.Value?.GetHashCode() ?? 0));

            this.entity = entity;
            this.PrimaryArguments = (this.primaryParameters ?? Enumerable.Empty<TemplateParameter>())
                .Select(it => this.table[it]).StoreReadOnlyList();

            if (this.PrimaryArguments.All(it => it == null))
                this.PrimaryArguments = Enumerable.Empty<IEntityInstance>().StoreReadOnlyList();
            else if (this.PrimaryArguments.Any(it => it == null))
                throw new NotImplementedException();
        }

        public override string ToString()
        {
            const int limit = 3;
            string s = table.Take(limit).Select(it => $"{it.Key} ==> {it.Value}").Join(", ");
            if (table.Count > limit)
                s += ", ...";
            return $"[{table.Count}: {s}]";
        }

        public static TemplateTranslation OverriddenTraitWithHostParameters(TemplateTranslation translation, TypeDefinition trait)
        {
            if (translation.table.Count == 0)
                return translation;

            return translation.overridden(trait.Name.Parameters, trait.AssociatedHost.Name.Parameters.Select(it => it.InstanceOf));
        }

        public static TemplateTranslation Translated(TemplateTranslation baseTranslation,
            TemplateTranslation through, ref bool translated)
        {
            if (baseTranslation == null || through == null)
                throw new ArgumentNullException();

            if (baseTranslation.table.Count == 0 || through.table.Count == 0)
                return baseTranslation;

            var dict = Later.Create(() => baseTranslation.table.ToDictionary(it => it.Key, it => it.Value));
            foreach (KeyValuePair<TemplateParameter, IEntityInstance> entry in baseTranslation.table)
            {
                if (entry.Value != null)
                {
                    bool trans = false;
                    IEntityInstance entityInstance = entry.Value.TranslateThrough(ref trans, through);

                    if (trans)
                        dict.Value[entry.Key] = entityInstance;
                }
                else if (through.Translate(entry.Key, out IEntityInstance value))
                {
                    dict.Value[entry.Key] = value;
                }
            }

            if (dict.HasValue)
            {
                translated = true;
                return new TemplateTranslation(baseTranslation.entity, dict.Value);
            }
            else
                return baseTranslation;
        }

        public TemplateTranslation Overridden(IEnumerable<IEntityInstance> arguments)
        {
            if (this.table.Count == 0)
                return this;

            return overridden(this.primaryParameters, arguments);
        }

        private TemplateTranslation overridden(IReadOnlyList<TemplateParameter> parameters, IEnumerable<IEntityInstance> arguments)
        {
            var dict = Later.Create(() => this.table.ToDictionary(it => it.Key, it => it.Value));
            int i = -1;
            foreach (IEntityInstance arg in arguments)
            {
                ++i;

                TemplateParameter key = parameters[i];
                if (!object.ReferenceEquals(this.table[key], arg))
                    dict.Value[key] = arg;
            }

            if (dict.HasValue)
                return new TemplateTranslation(this.entity, dict.Value);
            else
                return this;
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

            if (Object.ReferenceEquals(obj, null))
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
            return this.hashCode;
        }

        public bool Translate(TemplateParameter templateParameter, out IEntityInstance instanceArgument)
        {
            return this.table.TryGetValue(templateParameter, out instanceArgument) && instanceArgument != null;
        }

    }

}