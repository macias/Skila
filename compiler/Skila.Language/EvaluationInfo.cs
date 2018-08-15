using System;
using System.Diagnostics;
using Skila.Language.Expressions;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EvaluationInfo
    {
        public static EvaluationInfo Create(IEntityInstance components, EntityInstance merged)
        {
            return new EvaluationInfo(components, merged);
        }
        public static EvaluationInfo Create(EntityInstance merged)
        {
            return Create(merged, merged);
        }

        // combined the types in such way that all the members are valid for given combination of types
        // union of members for type intersection, and intersection of members for union of types (sic!)
        // consider type: X or Y (union)
        // such aggregate type has to have those members that are common to both types (intersection)
        // so for example call "x_or_y.callMe()" would always work no matter what we pass in runtime, x or y
        public EntityInstance Aggregate { get; }
        public IEntityInstance Components { get; }

        public EvaluationInfo(IEntityInstance components, EntityInstance merged)
        {
            if (components == null || merged == null)
                throw new ArgumentNullException();

            this.Components = components;
            this.Aggregate = merged;
        }
        public EvaluationInfo(EntityInstance eval) : this(eval, eval)
        {

        }

        public override string ToString()
        {
            string a = this.Aggregate.ToString();
            string c = this.Components.ToString();
            if (a == c)
                return c;
            else
                return c + " / " + a;
        }

        internal EvaluationInfo PromotLifetime(ComputationContext ctx, IOwnedNode node)
        {
            if (ctx.Env.IsPointerLikeOfType(this.Aggregate))
                return this;
            else
            {
                // for values we need to promote lifetimes, because if nested scope passes
                // value to outer one the lifetime of the value changes along with data passing
                Lifetime lifetime = Lifetime.Create(node, LifetimeScope.Local);

                return PromoteLifetime(ctx, lifetime);
            }
        }
        internal EvaluationInfo PromoteLifetime(ComputationContext ctx, Lifetime lifetime)
        {
            return EvaluationInfo.Create(this.Components.Rebuild(ctx, lifetime, deep: false), this.Aggregate.Build(lifetime));
        }
    }
}
