using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TemplateParameter : Node
    {
        public TypeDefinition AssociatedType { get; private set; }
        public EntityInstance InstanceOf { get; private set; }

        public int Index { get; }
        public string Name { get; }
        public VarianceMode Variance { get; }

        private TemplateConstraint constraint;
        // for example "const" meaning the type argument has to be immutable
        public EntityModifier ConstraintModifier => this.constraint.ConstraintModifier;
        public IReadOnlyCollection<NameReference> InheritsNames => this.constraint.InheritsNames;
        public IReadOnlyCollection<NameReference> BaseOfNames => this.constraint.BaseOfNames;
        public IReadOnlyCollection<FunctionDefinition> Functions => this.constraint.Functions;

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> OwnedNodes
        {
            get
            {
                if (this.constraint != null)
                    yield return this.constraint;
            }
        }

        public TemplateParameter(int index,
            string name,
            VarianceMode variance)
            : base()
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Index = index;
            this.Name = name;
            this.Variance = variance;
        }

        public void SetConstraint(TemplateConstraint constraint)
        {
            if (this.constraint != null)
                throw new InvalidOperationException();

            this.constraint = constraint ?? new TemplateConstraint(NameReference.Create(this.Name), null, null, null, null);

            this.AssociatedType = TypeDefinition.CreateTypeParameter(this);
            this.InstanceOf = AssociatedType.GetInstanceOf(null);

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string result = VarianceModeExtensions.ToString(this.Variance);
            if (result != "")
                result += " ";
            return result + Name;
        }

        public IEnumerable<EntityInstance> TranslateInherits(EntityInstance closedTemplate)
        {
            return InheritsNames.Select(it => it.Binding.Match)
                .WhereType<EntityInstance>()
                .Select(it => it.TranslateThrough(closedTemplate));
        }
        public IEnumerable<EntityInstance> TranslateBaseOf(EntityInstance closedTemplate)
        {
            return BaseOfNames.Select(it => it.Binding.Match)
                .WhereType<EntityInstance>()
                .Select(it => it.TranslateThrough(closedTemplate));
        }


    }
}
