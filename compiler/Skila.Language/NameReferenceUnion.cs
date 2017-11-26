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
    public sealed class NameReferenceUnion : Node, INameReference
    {
        public bool IsBindingComputed => this.Names.All(it => it.IsBindingComputed);
        public IReadOnlyCollection<INameReference> Names { get; }
        public override IEnumerable<INode> OwnedNodes => this.Names.Select(it => it.Cast<INode>())
            .Concat(this.aggregate)
            .Where(it => it != null);

        public bool IsComputed => this.Evaluation != null;

        private TypeDefinition aggregate;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsDereferenced { get; set; }

        private NameReferenceUnion(IEnumerable<INameReference> names)
        {
            this.Names = names.StoreReadOnly();
            if (!this.Names.Any())
                throw new ArgumentException();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public static NameReferenceUnion Create(IEnumerable<INameReference> names)
        {
            return new NameReferenceUnion(names);
        }
        public static NameReferenceUnion Create(params INameReference[] names)
        // union is a set, order does not matter
        {
            return new NameReferenceUnion(names);
        }

        public override string ToString()
        {
            return this.Names.Select(it => it.ToString()).Join("|");
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 2629)
                {
                    ;
                }
                IEntityInstance eval = EntityInstanceUnion.Create(Names.Select(it => it.Evaluation.Components));

                // check if we don't have both kinds of types -- slicing and non-slicing (for example Array and pointer to String)

                bool found_slicing_type = false;
                int found_non_slicing_type = 0;

                foreach (EntityInstance instance in eval.Enumerate())
                {
                    if (!instance.Target.IsType())
                        continue;

                    if (instance.TargetType.AllowSlicedSubstitution)
                        found_slicing_type = true;
                    else
                        ++found_non_slicing_type;

                    // we can have only multiple ref/ptr types, other mixes are not allowed
                    if ((found_slicing_type ? 1 : 0) + found_non_slicing_type > 1)
                    {
                        ctx.ErrorManager.AddError(ErrorCode.MixingSlicingTypes, this);
                        break;
                    }
                }


                {
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
                                .Where(f => !f.IsConstructor() && f.Parameters.All(it => !it.IsOptional))
                                .ToList();
                        else
                        {
                            foreach (FunctionDefinition m in members.ToArray())
                            {
                                bool found = false;
                                foreach (FunctionDefinition func in instance.TargetType.NestedFunctions)
                                {
                                    // todo: maybe some day handle optionals
                                    if (func.IsConstructor() || func.Parameters.Any(it => it.IsOptional))
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

                    this.aggregate = TypeBuilder.Create(ctx.AutoName.CreateNew("Aggregate"))
                        .With(members)
                        .Modifier(EntityModifier.Protocol);
                    aggregate.AttachTo(this);
                    this.aggregate.Evaluated(ctx);

                    EntityInstance aggregate_instance = this.aggregate.InstanceOf;
                    foreach (EntityInstance instance in dereferenced_instances)
                    {
                        EntityInstanceExtension.BuildDuckVirtualTable(ctx, instance, aggregate_instance);
                    }

                    if (has_reference)
                        aggregate_instance = ctx.Env.ReferenceType.GetInstanceOf(new[] { aggregate_instance }, overrideMutability: false);
                    else if (has_pointer)
                        aggregate_instance = ctx.Env.PointerType.GetInstanceOf(new[] { aggregate_instance }, overrideMutability: false);

                    this.Evaluation = new EvaluationInfo(eval, aggregate_instance);
                }


            }
        }

        public void Validate(ComputationContext ctx)
        {
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

    }

}