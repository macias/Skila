using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using Skila.Language.Printout;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReferenceIntersection : NameReferenceSet, INameReference
    {
        public static INameReference Create(IEnumerable<INameReference> names)
        {
            if (!names.Any())
                throw new ArgumentOutOfRangeException();

            if (names.Count() == 1)
                return names.Single();
            else
                return new NameReferenceIntersection(names);
        }
        public static NameReferenceIntersection Create(params INameReference[] names)
        // intersection is a set, order does not matter
        {
            return new NameReferenceIntersection(names);
        }

        private NameReferenceIntersection(IEnumerable<INameReference> names) : base(names)
        {
        }

        public override string ToString()
        {
            return this.Elements.Select(it => it.ToString()).Join("&");
        }

        protected override void compute(ComputationContext ctx)
        {
            IEntityInstance eval = EntityInstanceIntersection.Create(Elements.Select(it => it.Evaluation.Components));



            // we need to get sum (all) of the members

            bool has_reference = false;
            bool has_pointer = false;
            var dereferenced_instances = new List<EntityInstance>();
            List<FunctionDefinition> members = new List<FunctionDefinition>();
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

                foreach (FunctionDefinition func in instance.TargetType.NestedFunctions)
                {
                    bool found = false;
                    foreach (FunctionDefinition m in members)
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
                        members.Add(func);
                }
            }

            EntityInstance aggregate_instance = createAggregate(ctx, has_reference, has_pointer,
                dereferenced_instances, members, partialVirtualTables: true);

            this.Evaluation = EvaluationInfo.Create(eval, aggregate_instance);
        }

        // todo: this is copy from EntityInstanceIntersection, not cool, not cool, REUSE!
        protected override bool hasSymmetricRelation(INameReference other,
           Func<INameReference, INameReference, bool> relation)
        {
            Func<NameReferenceIntersection, INameReference, bool> check_all
                = (set, instance) => set.Elements.All(it => relation(instance, it));
            Func<NameReferenceIntersection, INameReference, bool> check_any
                = (set, instance) => set.Elements.Any(it => relation(instance, it));

            var other_set = other as NameReferenceIntersection;
            if (other_set == null)
                return check_all(this, other);
            else
                // when comparing two unions the rule is simple: each instance from this has to have its identical counterpart in the other union
                // and in reverse, each instance from the other has to have its counterpart in this union, so for example
                // Int|Int|String is identical with Int|String, but it is not with Int|Object|String
                return this.Elements.All(it => check_any(other_set, it))
                    && other_set.Elements.All(it => check_any(this, it));
        }
        public override ICode Printout()
        {
            return new CodeSpan().Append(this.Elements, "&");
        }
    }

}