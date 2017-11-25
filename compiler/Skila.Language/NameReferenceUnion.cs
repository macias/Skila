using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReferenceUnion : Node, INameReference
    {
        public bool IsBindingComputed => this.Names.All(it => it.IsBindingComputed);
        public IReadOnlyCollection<INameReference> Names { get; }
        public override IEnumerable<INode> OwnedNodes => this.Names;
        public bool IsComputed => this.Evaluation != null;

        public IEntityInstance Evaluation { get; private set; }
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
                this.Evaluation = EntityInstanceUnion.Create(Names.Select(it => it.Evaluation));

                // check if we don't have both kinds of types -- slicing and non-slicing (for example Array and pointer to String)

                bool found_slicing_type = false;
                int found_non_slicing_type = 0;

                foreach (EntityInstance instance in this.Evaluation.Enumerate())
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
        }

        public void Validate( ComputationContext ctx)
        {
        }

        public bool IsReadingValueOfNode( IExpression node)
        {
            return true;
        }

    }

}