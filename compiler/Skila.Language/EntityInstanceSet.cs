using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class EntityInstanceSet : IEntityInstance
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        // these are unique (see constructor)
        public IReadOnlyCollection<IEntityInstance> Instances { get; }
        public bool IsJoker => this.Instances.All(it => it.IsJoker);

        public INameReference NameOf { get; }

        public bool DependsOnTypeParameter_UNUSED => this.Instances.Any(it => it.DependsOnTypeParameter_UNUSED);

        protected EntityInstanceSet(IEnumerable<IEntityInstance> instances)
        {
            // we won't match against jokers here so we can use reference comparison
            // since all entity instances are singletons
            this.Instances = instances.Distinct(ReferenceEqualityComparer<IEntityInstance>.Instance).StoreReadOnly();
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
        public IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated)
        {
            bool trans = false;

            foreach (IEntityInstance closed in Instances)
                openTemplate = closed.TranslationOf(openTemplate, ref trans);

            if (trans)
                translated = true;

            return openTemplate;
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return this.Instances.All(it => it.IsValueType(ctx));
        }

        protected abstract IEntityInstance createNew(IEnumerable<IEntityInstance> instances);

        public abstract TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input,
            VarianceMode variance, bool allowSlicing);
        public abstract TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target,
                VarianceMode variance, bool allowSlicing);
        public abstract TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing);
        public abstract TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing);
        public abstract bool IsOverloadDistinctFrom(IEntityInstance other);


        public IEntityInstance TranslateThrough(EntityInstance closedTemplate, ref bool translated)
        {
            var result = new List<IEntityInstance>();
            bool trans = false;

            foreach (IEntityInstance open in Instances)
                result.Add(open.TranslateThrough(closedTemplate, ref trans));

            if (trans)
            {
                translated = true;
                return createNew(result);
            }
            else
                return this;
        }

        public ConstraintMatch ArgumentMatchesConstraintsOf(ComputationContext ctx, EntityInstance verifiedInstance,
            TemplateParameter param)
        {
            foreach (IEntityInstance arg in this.Instances)
            {
                ConstraintMatch match = arg.ArgumentMatchesConstraintsOf(ctx, verifiedInstance, param);
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



        public IEnumerable<EntityInstance> Enumerate()
        {
            return this.Instances.SelectMany(it => it.Enumerate());
        }
    }

}
