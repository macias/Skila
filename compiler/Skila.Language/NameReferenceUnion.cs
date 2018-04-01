using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Builders;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReferenceUnion : NameReferenceSet, INameReference
    {
        public static NameReferenceUnion Create(IEnumerable<INameReference> names)
        {
            return new NameReferenceUnion(names);
        }
        public static NameReferenceUnion Create(params INameReference[] names)
        // union is a set, order does not matter
        {
            return new NameReferenceUnion(names);
        }

        private NameReferenceUnion(IEnumerable<INameReference> names) : base(names)
        {
        }

        protected override void compute(ComputationContext ctx)
        {
            IEntityInstance eval = EntityInstanceUnion.Create(Elements.Select(it => it.Evaluation.Components));

            // we need to get common members

            bool has_reference = false;
            bool has_pointer = false;
            var dereferenced_instances = new List<EntityInstance>();
            List<FunctionDefinition> members = null;
            foreach (EntityInstance ____instance in this.Elements.Select(it => it.Evaluation.Aggregate))
            {
                if (ctx.Env.DereferencedOnce(____instance, out IEntityInstance __instance, out bool via_pointer))
                {
                    if (via_pointer)
                        has_pointer = true;
                    else
                        has_reference = true;
                }

                EntityInstance instance = __instance.Cast<EntityInstance>();

                dereferenced_instances.Add(instance);

                if (members == null)
                    members = instance.TargetType.NestedFunctions
                        .Where(f => !f.IsAnyConstructor() && f.Parameters.All(it => !it.IsOptional))
                        .ToList();
                else
                {
                    foreach (FunctionDefinition m in members.ToArray())
                    {
                        bool found = false;
                        foreach (FunctionDefinition func in instance.TargetType.NestedFunctions)
                        {
                            // todo: maybe some day handle optionals
                            if (func.IsAnyConstructor() || func.Parameters.Any(it => it.IsOptional))
                                continue;

                            if (FunctionDefinitionExtension.IsSame(ctx, m, func, instance))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (!found)
                            members.Remove(m);
                    }
                }

            }

            EntityInstance aggregate_instance = createAggregate(ctx, has_reference, has_pointer,
                dereferenced_instances, members, partialVirtualTables: false);

            this.Evaluation = new EvaluationInfo(eval, aggregate_instance);

        }
        protected override bool hasSymmetricRelation(INameReference other,
          Func<INameReference, INameReference, bool> relation)
        {
            Func<NameReferenceUnion, INameReference, bool> check_all
                = (union, instance) => union.Elements.All(it => relation(instance, it));
            Func<NameReferenceUnion, INameReference, bool> check_any
                = (union, instance) => union.Elements.Any(it => relation(instance, it));

            var other_union = other as NameReferenceUnion;
            if (other_union == null)
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.Elements.All(it => check_any(other_union, it))
                    && other_union.Elements.All(it => check_any(this, it));
        }
    }

}