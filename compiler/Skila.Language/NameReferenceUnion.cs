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
            if (this.DebugId.Id == 2629)
            {
                ;
            }

            IEntityInstance eval = EntityInstanceUnion.Create(Names.Select(it => it.Evaluation.Components));

            // we need to get common members

            bool has_reference = false;
            bool has_pointer = false;
            var dereferenced_instances = new List<EntityInstance>();
            List<FunctionDefinition> members = null;
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
    }

}