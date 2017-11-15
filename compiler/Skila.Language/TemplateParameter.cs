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

        public TemplateConstraint Constraint { get; private set; }

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> OwnedNodes
        {
            get
            {
                if (this.Constraint != null)
                    yield return this.Constraint;
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
            if (this.Constraint != null)
                throw new InvalidOperationException();

            this.Constraint = constraint ?? new TemplateConstraint(NameReference.Create(this.Name), null, null, null, null);

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
            return this.Constraint.InheritsNames.Select(it => it.Binding.Match)
                .WhereType<EntityInstance>()
                .Select(it => it.TranslateThrough(closedTemplate));
        }
        public IEnumerable<EntityInstance> TranslateBaseOf(EntityInstance closedTemplate)
        {
            return this.Constraint.BaseOfNames.Select(it => it.Binding.Match)
                .WhereType<EntityInstance>()
                .Select(it => it.TranslateThrough(closedTemplate));
        }

    }
}
