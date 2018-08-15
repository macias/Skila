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
            if (!instances.Any())
                throw new ArgumentOutOfRangeException();

            if (instances.Count() == 1)
                return instances.Single();
            else
                return new EntityInstanceIntersection(instances);
        }

        private EntityInstanceIntersection(IEnumerable<IEntityInstance> instances) : base(instances)
        {
        }

        public override string ToString()
        {
            return this.elements.Select(it => it.ToString()).Join("&");
        }

        protected override bool hasSymmetricRelation(IEntityInstance other,
            Func<IEntityInstance, IEntityInstance, bool> relation)
        {
            Func<EntityInstanceIntersection, IEntityInstance, bool> check_all
                = (set, instance) => set.elements.All(it => relation(instance, it));
            Func<EntityInstanceIntersection, IEntityInstance, bool> check_any
                = (set, instance) => set.elements.Any(it => relation(instance, it));

            var other_set = other as EntityInstanceIntersection;
            if (other_set == null)
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.elements.All(it => check_any(other_set, it))
                    && other_set.elements.All(it => check_any(this, it));
        }

        protected override IEntityInstance createNew(IEnumerable<IEntityInstance> instances)
        {
            return Create(instances);
        }


        public override TypeMatch TemplateMatchesInput(ComputationContext ctx,
            EntityInstance input, VarianceMode variance, TypeMatching matching)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.elements)
            {
                TypeMatch m = target.TemplateMatchesInput(ctx, input, variance, matching);
                if (m.IsMismatch())
                    return m;
                else if (match.IsMismatch())
                    match = m;
                else if (match != m)
                    return TypeMatch.No;
            }

            return match;
        }

        public override TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, TypeMatching matching)
        {
            TypeMatch match = TypeMatch.No;
            foreach (IEntityInstance target in this.elements)
            {
                TypeMatch m = target.MatchesInput(ctx, input, matching);
                if (m.IsMismatch())
                    return m;
                else if (match.IsMismatch())
                    match = m;
                else if (match != m)
                    return TypeMatch.No;
            }

            return match;
        }

        // this is somewhat limiting, because when we have multiple targets we go easy way not allowing
        // type conversion, some day improve it
        public override TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, TypeMatching matching)
        {
            IEnumerable<TypeMatch> matches = this.elements.Select(it => it.MatchesTarget(ctx, target, matching)).ToArray();
            if (matches.Any(it => it == TypeMatch.Same))
                return TypeMatch.Same;

            IEnumerable<TypeMatch> substitutions = matches.Where(it => it == TypeMatch.Substitute).OrderBy(it => it.Distance);
            if (substitutions.Any())
                return substitutions.First();
            else
                return TypeMatch.No;
        }

        public override TypeMatch TemplateMatchesTarget(ComputationContext ctx,
            IEntityInstance target, VarianceMode variance, TypeMatching matching)
        {
            IEnumerable<TypeMatch> matches = this.elements.Select(it => it.TemplateMatchesTarget(ctx, target, variance, matching));
            if (matches.Any(it => it == TypeMatch.Same))
                return TypeMatch.Same;

            IEnumerable<TypeMatch> substitutions = matches.Where(it => it == TypeMatch.Substitute).OrderBy(it => it.Distance);
            if (substitutions.Any())
                return substitutions.First();
            else
                return TypeMatch.No;
        }

        public override bool IsOverloadDistinctFrom(IEntityInstance other)
        {
            // consider functions foo(String) and foo(Int|String) 
            // this is incorrect overload because String value matches both in the same perfect way
            // so we need to have all elements to be distinct
            return this.elements.Any(it => it.IsOverloadDistinctFrom(other));
        }

        public override IEntityInstance Map(Func<EntityInstance, IEntityInstance> func)
        {
            return new EntityInstanceIntersection(this.elements.Select(it => it.Map(func)));
        }
    }

}
