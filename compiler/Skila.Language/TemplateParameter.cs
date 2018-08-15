using System;
using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Printout;
using Skila.Language.Tools;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TemplateParameter : OwnedNode, IPrintable
    {
        public TypeDefinition AssociatedType { get; private set; }
        public EntityInstance InstanceOf { get; private set; }

        public int Index { get; }
        public string Name { get; }
        public VarianceMode Variance { get; }

        public TemplateConstraint Constraint { get; private set; }

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> ChildrenNodes
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

            this.Constraint = constraint ?? TemplateConstraint.Create(NameReference.Create(this.Name), null, null, null, null);

            this.AssociatedType = TypeDefinition.CreateTypeParameter(this);
            this.InstanceOf = AssociatedType.GetInstance(null, overrideMutability: TypeMutability.None, translation: null, lifetime: Lifetime.Timeless);

            this.attachPostConstructor();
        }

        public override string ToString()
        {
            return Printout().ToString();
        }
        public ICode Printout()
        {
            string var_str = VarianceModeExtensions.ToString(this.Variance);
            if (var_str != "")
                var_str += " ";
            return new CodeText(var_str + Name);
        }
    }
}
