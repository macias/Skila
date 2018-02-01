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
        public HashSet<IEntityInstance> Instances { get; }
        public bool IsJoker => this.Instances.All(it => it.IsJoker);

        public INameReference NameOf { get; }

        protected EntityInstanceSet(IEnumerable<IEntityInstance> instances)
        {
#if DEBUG
            DebugId = new DebugId(this.GetType());
#endif

            // we won't match against jokers here so we can use reference comparison
            // since all entity instances are singletons
            this.Instances = instances.ToHashSet(EntityInstanceCoreComparer.Instance);
            this.NameOf = NameReferenceUnion.Create(this.Instances.Select(it => it.NameOf));
            if (!this.Instances.Any())
                throw new ArgumentException();
        }

        protected abstract bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation);

        public bool IsSame(IEntityInstance other, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

            return hasSymmetricRelation(other, (a, b) => a.IsSame(b, jokerMatchesAll));
        }
        public IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated, TemplateTranslation closedTranslation)
        {
            bool trans = false;

            foreach (IEntityInstance closed in Instances)
                openTemplate = closed.TranslationOf(openTemplate, ref trans, closedTranslation);

            if (trans)
                translated = true;

            return openTemplate;
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return this.Instances.All(it => it.IsValueType(ctx));
        }

        protected abstract IEntityInstance createNew(IEnumerable<IEntityInstance> instances);

        public abstract TypeMatch TemplateMatchesInput(ComputationContext ctx, EntityInstance input,
            VarianceMode variance, TypeMatching matching);
        public abstract TypeMatch TemplateMatchesTarget(ComputationContext ctx, IEntityInstance target,
                VarianceMode variance, TypeMatching matching);
        public abstract TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing);
        public abstract TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing);
        public abstract bool IsOverloadDistinctFrom(IEntityInstance other);
        public abstract IEntityInstance Map(Func<EntityInstance, IEntityInstance> func);

        public IEntityInstance TranslateThrough(ref bool translated, TemplateTranslation closedTranslation)
        {
            var result = new List<IEntityInstance>();
            bool trans = false;

            foreach (IEntityInstance open in Instances)
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
            foreach (IEntityInstance arg in this.Instances)
            {
                ConstraintMatch match = arg.ArgumentMatchesParameterConstraints(ctx, closedTemplate, param);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }

        public bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor)
        {
            foreach (IEntityInstance instance in this.Instances)
                if (!instance.IsStrictDescendantOf(ctx, ancestor))
                    return false;

            return true;
        }
        public bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant)
        {
            foreach (IEntityInstance instance in this.Instances)
                if (!instance.IsStrictAncestorOf(ctx, descendant))
                    return false;

            return true;
        }

        public bool CoreEquals(IEntityInstance other)
        {
            if (this.GetType() != other.GetType())
                return false;

            EntityInstanceSet entity_set = other.Cast<EntityInstanceSet>();
            return this.Instances.SetEquals(entity_set.Instances);
        }


        public IEnumerable<EntityInstance> EnumerateAll()
        {
            return this.Instances.SelectMany(it => it.EnumerateAll());
        }
    }

}
