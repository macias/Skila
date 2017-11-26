using Skila.Language.Semantics;
using System;

namespace Skila.Language
{
    public sealed class EvaluationInfo
    {
        public static readonly EvaluationInfo Joker = new EvaluationInfo(EntityInstance.Joker);

        // combined the types in such way that all the members are valid for given combination of types
        // union of members for type intersection, and intersection of members for union of types (sic!)
        // consider type: X or Y (union)
        // such aggregate type has to have those members that are common to both types (intersection)
        // so for example call "x_or_y.callMe()" would always work no matter what we pass in runtime, x or y
        public EntityInstance Aggregate { get; }
        public IEntityInstance Components { get; }

        public EvaluationInfo(IEntityInstance components,EntityInstance merged)
        {
            if (components == null || merged == null)
                throw new ArgumentNullException();

            this.Components = components;
            this.Aggregate = merged;
        }
        public EvaluationInfo(EntityInstance eval) : this(eval, eval)
        {

        }
    }

}
