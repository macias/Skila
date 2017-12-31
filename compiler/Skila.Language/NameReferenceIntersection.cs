using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Builders;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReferenceIntersection : NameReferenceSet, INameReference
    {
        public static NameReferenceIntersection Create(IEnumerable<INameReference> names)
        {
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
            return this.Names.Select(it => it.ToString()).Join("&");
        }

        protected override void compute(ComputationContext ctx)
        {
            if (this.DebugId.Id == 2629)
            {
                ;
            }
            IEntityInstance eval = EntityInstanceIntersection.Create(Names.Select(it => it.Evaluation.Components));



            // we need to get sum (all) of the members

            bool has_reference = false;
            bool has_pointer = false;
            var dereferenced_instances = new List<EntityInstance>();
            List<FunctionDefinition> members = new List<FunctionDefinition>();
            foreach (EntityInstance ____instance in this.Names.Select(it => it.Evaluation.Aggregate))
            {
                if (ctx.Env.Dereferenced(____instance, out IEntityInstance __instance, out bool via_pointer))
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

            this.Evaluation = new EvaluationInfo(eval, aggregate_instance);
        }
    }

}