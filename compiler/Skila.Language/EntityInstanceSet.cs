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
        protected readonly HashSet<IEntityInstance> elements;
        public bool IsJoker => this.elements.All(it => it.IsJoker);

        private TypeMutability? typeMutability;
        private TypeMutability? directTypeMutability;

        public INameReference NameOf { get; }
        public INameReference PureNameOf { get; }

        protected EntityInstanceSet(IEnumerable<IEntityInstance> instances)
        {
#if DEBUG
            DebugId = new DebugId(this.GetType());
#endif

            // we won't match against jokers here so we can use reference comparison
            // since all entity instances are singletons
            this.elements = instances.ToHashSet(EntityInstance.ComparerI);
            this.NameOf = NameReferenceUnion.Create(this.elements.Select(it => it.NameOf));
            this.PureNameOf = NameReferenceUnion.Create(this.elements.Select(it => it.PureNameOf));
            if (!this.elements.Any())
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

            foreach (IEntityInstance closed in elements)
                openTemplate = closed.TranslationOf(openTemplate, ref trans, closedTranslation);

            if (trans)
                translated = true;

            return openTemplate;
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return this.elements.All(it => it.IsValueType(ctx));
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

            foreach (IEntityInstance open in elements)
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
            foreach (IEntityInstance arg in this.elements)
            {
                ConstraintMatch match = arg.ArgumentMatchesParameterConstraints(ctx, closedTemplate, param);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }

        public bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor)
        {
            foreach (IEntityInstance instance in this.elements)
                if (!instance.IsStrictDescendantOf(ctx, ancestor))
                    return false;

            return true;
        }
        public bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant)
        {
            foreach (IEntityInstance instance in this.elements)
                if (!instance.IsStrictAncestorOf(ctx, descendant))
                    return false;

            return true;
        }

        public bool IsIdentical(IEntityInstance other)
        {
            if (this.GetType() != other.GetType())
                return false;

            EntityInstanceSet entity_set = other.Cast<EntityInstanceSet>();
            return this.elements.SetEquals(entity_set.elements);
        }


        public IEnumerable<EntityInstance> EnumerateAll()
        {
            return this.elements.SelectMany(it => it.EnumerateAll());
        }

        public TypeMutability MutabilityOfType(ComputationContext ctx)
        {
            if (!this.typeMutability.HasValue)
                this.typeMutability = this.ComputeMutabilityOfType(ctx, new HashSet<IEntityInstance>(EntityInstance.ComparerI));
            return this.typeMutability.Value;
        }
        public TypeMutability SurfaceMutabilityOfType(ComputationContext ctx)
        {
            if (!this.directTypeMutability.HasValue)
                this.directTypeMutability = this.ComputeSurfaceMutabilityOfType(ctx);
            return this.directTypeMutability.Value;
        }

        public bool ValidateTypeVariance(ComputationContext ctx, INode placement, VarianceMode typeNamePosition)
        {
            return this.EnumerateAll().All(it => it.ValidateTypeVariance(ctx,
                       placement,
                       typeNamePosition));
        }

        public override int GetHashCode()
        {
            return this.EnumerateAll().Aggregate(0, (acc, a) => acc ^ a.GetHashCode());
        }
    }
}