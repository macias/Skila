using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    // expresses the notion of type that can be type A and type B and ..., for example
    // x (*Double and *String)
    // "x" defined this way could be taken square root of and at the same time searched for substring
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstanceIntersection : EntityInstanceSet
    {
        public static IEntityInstance Create(IEnumerable<IEntityInstance> instances)
        {
            return new EntityInstanceIntersection(instances);
        }

        private EntityInstanceIntersection(IEnumerable<IEntityInstance> instances) :base(instances)
        {
        }

        public override string ToString()
        {
            return this.Instances.Select(it => it.ToString()).Join("&");
        }

        protected override bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation)
        {
            Func<EntityInstanceIntersection, IEntityInstance, bool> check_all 
                = (set, instance) => set.Instances.All(it => relation(instance, it));
            Func<EntityInstanceIntersection, IEntityInstance, bool> check_any 
                = (set, instance) => set.Instances.Any(it => relation(instance, it));

            var other_set = other as EntityInstanceIntersection;
            if (other_set == null)
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.Instances.All(it => check_any(other_set, it)) 
                    && other_set.Instances.All(it => check_any(this, it));
        }

        protected override IEntityInstance createNew(IEnumerable<IEntityInstance> instances)
        {
            return Create(instances);
        }

     
        public override TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, 
            EntityInstance input, VarianceMode variance, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.TemplateMatchesInput(ctx, inversedVariance, input, variance, allowSlicing);
                if (m == TypeMatch.No)
                    return TypeMatch.No;
                else if (match == TypeMatch.No)
                    match = m;
                else if (match != m)
                    return TypeMatch.No;
            }

            return match;
        }

        public override TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.Instances)
            {
                TypeMatch m = target.MatchesInput(ctx, input, allowSlicing);
                if (m == TypeMatch.No)
                    return TypeMatch.No;
                else if (match == TypeMatch.No)
                    match = m;
                else if (match != m)
                    return TypeMatch.No;
            }

            return match;
        }

        // this is somewhat limiting, because when we have multiple targets we go easy way not allowing
        // type conversion, some day improve it
        public override TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing)
        {
            IEnumerable<TypeMatch> matches = this.Instances.Select(it => it.MatchesTarget(ctx, target, allowSlicing)).ToArray();
            if (matches.Any(it => it == TypeMatch.Same))
                return TypeMatch.Same;
            else if (matches.Any(it => it== TypeMatch.Substitute))
                return TypeMatch.Substitute;
            else
                return TypeMatch.No;
        }

        public override TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, 
            IEntityInstance target, VarianceMode variance, bool allowSlicing)
        {
            IEnumerable<TypeMatch> matches = this.Instances.Select(it => it.TemplateMatchesTarget(ctx, inversedVariance, target, variance, allowSlicing));
            if (matches.Any(it => it == TypeMatch.Same))
                return TypeMatch.Same;
            else if (matches.Any(it => it == TypeMatch.Substitute))
                return TypeMatch.Substitute;
            else
                return TypeMatch.No;
        }

        public override bool IsOverloadDistinctFrom(IEntityInstance other)
        {
            // consider functions foo(String) and foo(Int|String) 
            // this is incorrect overload because String value matches both in the same perfect way
            // so we need to have all elements to be distinct
            return this.Instances.Any(it => it.IsOverloadDistinctFrom(other));
        }

    }

}
