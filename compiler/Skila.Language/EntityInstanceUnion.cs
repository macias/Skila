using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    // expresses the notion of type that can be type A or type B or ..., for example
    // x (*Double or *String)
    // "x" defined this way could NOT be taken square root of or searched for substring because neither of those are defined in both types
    // but "x" could be added to another "x" assuming "add" operation is common to both of them
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstanceUnion : EntityInstanceSet
    {
        public static IEntityInstance Create(IEnumerable<IEntityInstance> instances)
        {
            return new EntityInstanceUnion(instances);
        }

        private EntityInstanceUnion(IEnumerable<IEntityInstance> instances) : base(instances)
        {
        }

        public override string ToString()
        {
            return this.Instances.Select(it => it.ToString()).Join("|");
        }

        protected override bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation)
        {
            Func<EntityInstanceUnion, IEntityInstance, bool> check_all
                = (union, instance) => union.Instances.All(it => relation(instance, it));
            Func<EntityInstanceUnion, IEntityInstance, bool> check_any
                = (union, instance) => union.Instances.Any(it => relation(instance, it));

            var other_union = other as EntityInstanceUnion;
            if (other_union == null)
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.Instances.All(it => check_any(other_union, it))
                    && other_union.Instances.All(it => check_any(this, it));
        }

        protected override IEntityInstance createNew(IEnumerable<IEntityInstance> instances)
        {
            return Create(instances);
        }


        public override TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input, VarianceMode variance, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.TemplateMatchesInput(ctx, inversedVariance, input, variance, allowSlicing);
                if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                    return m;
                else if (m == TypeMatch.AutoDereference
                    || m == TypeMatch.InConversion
                    || m == TypeMatch.ImplicitReference
                    || m == TypeMatch.OutConversion)
                    match = m;
                else if (m != TypeMatch.No)
                    throw new NotImplementedException();
            }

            return match;
        }

        public override TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.MatchesInput(ctx, input, allowSlicing);
                if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                    return m;
                else if (m == TypeMatch.InConversion
                    || m == TypeMatch.AutoDereference
                    || m == TypeMatch.ImplicitReference
                    || m == TypeMatch.OutConversion)
                    match = m;
                else if (m != TypeMatch.No)
                    throw new NotImplementedException();
            }

            return match;
        }

        // this is somewhat limiting, because when we have multiple targets we go easy way not allowing
        // type conversion, some day improve it
        public override TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing)
        {
            IEnumerable<TypeMatch> matches = this.Instances.Select(it => it.MatchesTarget(ctx, target, allowSlicing)).ToArray();
            if (matches.All(it => it == TypeMatch.Same))
                return TypeMatch.Same;

            IEnumerable<TypeMatch> substitutions = matches.Where(it => it == TypeMatch.Same || it == TypeMatch.Substitute)
                .OrderByDescending(it => it.Distance);
            if (substitutions.Count() == matches.Count())
                return substitutions.First();
            else
                return TypeMatch.No;
        }

        public override TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target, VarianceMode variance, bool allowSlicing)
        {
            IEnumerable<TypeMatch> matches = this.Instances.Select(it => it.TemplateMatchesTarget(ctx, inversedVariance, target, variance, allowSlicing));
            if (matches.All(it => it == TypeMatch.Same))
                return TypeMatch.Same;

            IEnumerable<TypeMatch> substitutions = matches.Where(it => it==TypeMatch.Same || it == TypeMatch.Substitute)
                .OrderByDescending(it => it.Distance);
            if (substitutions.Count()==matches.Count())
                return substitutions.First();
            else
                return TypeMatch.No;
        }

        public override bool IsOverloadDistinctFrom(IEntityInstance other)
        {
            // consider functions foo(String) and foo(Int|String) 
            // this is incorrect overload because String value matches both in the same perfect way
            // so we need to have all elements to be distinct
            return this.Instances.All(it => it.IsOverloadDistinctFrom(other));
        }

    }

}
