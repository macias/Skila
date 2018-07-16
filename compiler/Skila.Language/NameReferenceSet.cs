using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Builders;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class NameReferenceSet : Node, INameReference
    {
        public bool IsBindingComputed => this.Elements.All(it => it.IsBindingComputed);
        public IReadOnlyCollection<INameReference> Elements { get; }
        public override IEnumerable<INode> OwnedNodes => this.Elements.Select(it => it.Cast<INode>())
            .Concat(this.aggregate)
            .Where(it => it != null);

        public bool IsSurfed { get; set; }

        public bool IsComputed { get; protected set; }

        private TypeDefinition aggregate;
        public EvaluationInfo Evaluation { get; protected set; }
        public ValidationData Validation { get; set; }
        public bool IsDereferenced { get; set; }

        public abstract ICode Printout();

        protected NameReferenceSet(IEnumerable<INameReference> names)
        {
            this.Elements = names.StoreReadOnly();
            if (!this.Elements.Any())
                throw new ArgumentException();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                compute(ctx);

            this.IsComputed = true;
        }


        public void Validate(ComputationContext ctx)
        {
            // check if we don't have both kinds of types -- slicing and non-slicing (for example Array and pointer to String)

            bool found_slicing_type = false;
            int found_non_slicing_type = 0;

            foreach (EntityInstance instance in this.Evaluation.Components.EnumerateAll())
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
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        protected EntityInstance createAggregate(ComputationContext ctx, bool hasReference, bool hasPointer,
            IEnumerable<EntityInstance> dereferencedInstances, IEnumerable<FunctionDefinition> members,
            bool partialVirtualTables)
        {
            this.aggregate = TypeBuilder.Create(AutoName.Instance.CreateNew("Aggregate"))
                .With(members)
                .SetModifier(EntityModifier.Protocol);
            aggregate.AttachTo(this);
            this.aggregate.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

            EntityInstance aggregate_instance = this.aggregate.InstanceOf;
            foreach (EntityInstance instance in dereferencedInstances)
            {
                EntityInstanceExtension.BuildDuckVirtualTable(ctx, instance, aggregate_instance, allowPartial: partialVirtualTables);
            }

            if (hasReference || hasPointer)
                aggregate_instance = ctx.Env.Reference(aggregate_instance, TypeMutability.None,
                    translation: null, viaPointer: hasPointer);

            return aggregate_instance;
        }

        protected abstract void compute(ComputationContext ctx);

        public void Surf(ComputationContext ctx)
        {
            this.OwnedNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));

            compute(ctx);
        }

        protected abstract bool hasSymmetricRelation(INameReference other,
          Func<INameReference, INameReference, bool> relation);

        public bool IsExactlySame(INameReference other, EntityInstance translationTemplate, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

            return hasSymmetricRelation(other, (a, b) => a.IsExactlySame(b,translationTemplate, jokerMatchesAll));
        }
    }

}