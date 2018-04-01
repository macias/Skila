using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Comparers;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class EntityInstanceSet : IEntityInstance
    {
#if DEBUG
        public DebugId DebugId { get; }
#endif

        // these are unique (see constructor)
        public HashSet<IEntityInstance> Elements { get; }
        public bool IsJoker => this.Elements.All(it => it.IsJoker);

        private TypeMutability? typeMutability;

        public INameReference NameOf { get; }

        protected EntityInstanceSet(IEnumerable<IEntityInstance> instances)
        {
#if DEBUG
            DebugId = new DebugId(this.GetType());
#endif

            // we won't match against jokers here so we can use reference comparison
            // since all entity instances are singletons
            this.Elements = instances.ToHashSet(EntityInstanceCoreComparer.Instance);
            this.NameOf = NameReferenceUnion.Create(this.Elements.Select(it => it.NameOf));
            if (!this.Elements.Any())
                throw new ArgumentException();
        }

        protected abstract bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation);

        public bool IsExactlySame(IEntityInstance other, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

            return hasSymmetricRelation(other, (a, b) => a.IsExactlySame(b, jokerMatchesAll));
        }
        public bool HasExactlySameTarget(IEntityInstance other, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

            return hasSymmetricRelation(other, (a, b) => a.HasExactlySameTarget(b, jokerMatchesAll));
        }
        public IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated, TemplateTranslation closedTranslation)
        {
            bool trans = false;

            foreach (IEntityInstance closed in Elements)
                openTemplate = closed.TranslationOf(openTemplate, ref trans, closedTranslation);

            if (trans)
                translated = true;

            return openTemplate;
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return this.Elements.All(it => it.IsValueType(ctx));
        }

        protected abstract IEntityInstance createNew(IEnumerable<IEntityInstance> instances);

        public abstract TypeMatch TemplateMatchesInput(ComputationContext ctx, EntityInstance input,
            VarianceMode variance, TypeMatching matching);
        public abstract TypeMatch TemplateMatchesTarget(ComputationContext ctx, IEntityInstance target,
                VarianceMode variance, TypeMatching matching);
        public abstract TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, TypeMatching matching);
        public abstract TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, TypeMatching matching);
        public abstract bool IsOverloadDistinctFrom(IEntityInstance other);
        public abstract IEntityInstance Map(Func<EntityInstance, IEntityInstance> func);

        public IEntityInstance TranslateThrough(ref bool translated, TemplateTranslation closedTranslation)
        {
            var result = new List<IEntityInstance>();
            bool trans = false;

            foreach (IEntityInstance open in Elements)
                result.Add(open.TranslateThrough(ref trans, closedTranslation));

            if (trans)
            {
                translated = true;
                return createNew(result);
            }
            else
                return this;
        }

        public ConstraintMatch ArgumentMatchesParameterConstraints(ComputationContext ctx, EntityInstance closedTemplate,
            TemplateParameter param)
        {
            foreach (IEntityInstance arg in this.Elements)
            {
                ConstraintMatch match = arg.ArgumentMatchesParameterConstraints(ctx, closedTemplate, param);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }

        public bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor)
        {
            foreach (IEntityInstance instance in this.Elements)
                if (!instance.IsStrictDescendantOf(ctx, ancestor))
                    return false;

            return true;
        }
        public bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant)
        {
            foreach (IEntityInstance instance in this.Elements)
                if (!instance.IsStrictAncestorOf(ctx, descendant))
                    return false;

            return true;
        }

        public bool CoreEquals(IEntityInstance other)
        {
            if (this.GetType() != other.GetType())
                return false;

            EntityInstanceSet entity_set = other.Cast<EntityInstanceSet>();
            return this.Elements.SetEquals(entity_set.Elements);
        }


        public IEnumerable<EntityInstance> EnumerateAll()
        {
            return this.Elements.SelectMany(it => it.EnumerateAll());
        }

        public TypeMutability MutabilityOfType(ComputationContext ctx)
        {
            if (!this.typeMutability.HasValue)
                this.typeMutability = this.ComputeMutabilityOfType(ctx, new HashSet<IEntityInstance>());
            return this.typeMutability.Value;
        }
    }

}
