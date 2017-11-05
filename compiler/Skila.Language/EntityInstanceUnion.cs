using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    // expresses the notion of type that can be type A or type B or ...
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstanceUnion : IEntityInstance
    {
        public static IEntityInstance Create(IEnumerable<IEntityInstance> instances)
        {
            return new EntityInstanceUnion(instances);
        }

#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        public IReadOnlyCollection<IEntityInstance> Instances { get; }
        public bool IsJoker => this.Instances.All(it => it.IsJoker);

        public INameReference NameOf { get; }

        public bool DependsOnTypeParameter_UNUSED => this.Instances.Any(it => it.DependsOnTypeParameter_UNUSED);

        private EntityInstanceUnion(IEnumerable<IEntityInstance> instances)
        {
            this.NameOf = NameReferenceUnion.Create(instances.Select(it => it.NameOf));
            this.Instances = instances.StoreReadOnly();
            if (!this.Instances.Any())
                throw new ArgumentException();
        }

        public override string ToString()
        {
            return this.Instances.Select(it => it.ToString()).Join("|");
        }

        private bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation)
        {
            Func<EntityInstanceUnion, IEntityInstance, bool> check_all = (union, instance) => union.Instances.All(it => relation(instance, it));
            Func<EntityInstanceUnion, IEntityInstance, bool> check_any = (union, instance) => union.Instances.Any(it => relation(instance, it));

            var other_union = other as EntityInstanceUnion;
            if (other_union == null)
                // please note that String is identical with String|String or String|String|String
                // but it is not with String|Not-String
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.Instances.All(it => check_any(other_union, it)) && other_union.Instances.All(it => check_any(this, it));
        }
        public bool IsSame(IEntityInstance other, bool jokerMatchesAll)
        {
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

        public IEntityInstance TranslateThrough(EntityInstance closedTemplate, ref bool translated)
        {
            var result = new List<IEntityInstance>();
            bool trans = false;

            foreach (IEntityInstance open in Instances)
                result.Add(open.TranslateThrough(closedTemplate, ref trans));

            if (trans)
            {
                translated = true;
                return new EntityInstanceUnion(result);
            }
            else
                return this;
        }

        public ConstraintMatch ArgumentMatchesConstraintsOf(ComputationContext ctx, EntityInstance verifiedInstance, TemplateParameter param)
        {
            foreach (IEntityInstance arg in this.Instances)
            {
                ConstraintMatch match = arg.ArgumentMatchesConstraintsOf(ctx, verifiedInstance, param);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }
        public TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input, VarianceMode variance, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.TemplateMatchesInput(ctx, inversedVariance, input, variance, allowSlicing);
                if (m == TypeMatch.Pass)
                    return m;
                else if (m == TypeMatch.AutoDereference)
                    match = m;
                else if (m == TypeMatch.InConversion)
                    match = m;
                else if (m == TypeMatch.ImplicitReference)
                    match = m;
                else if (m == TypeMatch.OutConversion)
                    match = m;
                else if (m != TypeMatch.No)
                    throw new NotImplementedException();
            }

            return match;
        }

        public TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.MatchesInput(ctx, input, allowSlicing);
                if (m == TypeMatch.Pass)
                    return m;
                else if (m == TypeMatch.InConversion)
                    match = m;
                else if (m == TypeMatch.AutoDereference)
                    match = m;
                else if (m == TypeMatch.ImplicitReference)
                    match = m;
                else if (m == TypeMatch.OutConversion)
                    match = m;
                else if (m != TypeMatch.No)
                    throw new NotImplementedException();
            }

            return match;
        }

        // this is somewhat limiting, because when we have multiple targets we go easy way not allowing
        // type conversion, some day improve it
        public TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing)
        {
            return this.Instances.All(it => it.MatchesTarget(ctx, target, allowSlicing) == TypeMatch.Pass) ? TypeMatch.Pass : TypeMatch.No;
        }

        public TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target, VarianceMode variance, bool allowSlicing)
        {
            return this.Instances.All(it => it.TemplateMatchesTarget(ctx, inversedVariance, target, variance, allowSlicing) == TypeMatch.Pass) ? TypeMatch.Pass : TypeMatch.No;
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

        public bool IsOverloadDistinctFrom(IEntityInstance other)
        {
            // consider functions foo(String) and foo(Int|String) 
            // this is incorrect overload because String value matches both in the same perfect way
            // so we need to have all elements to be distinct
            return this.Instances.All(it => it.IsOverloadDistinctFrom(other));
        }

        public IEnumerable<EntityInstance> Enumerate()
        {
            return this.Instances.SelectMany(it => it.Enumerate());
        }
    }

}
