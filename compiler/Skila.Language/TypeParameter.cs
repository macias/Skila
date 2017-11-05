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
        public TypeDefinition AssociatedType { get; }
        public EntityInstance InstanceOf { get; }

        public int Index { get; }
        public string Name { get; }
        public VarianceMode Variance { get; }
        // for example "const" meaning the type argument has to be immutable
        public EntityModifier ConstraintModifier { get; }
        public IReadOnlyCollection<NameReference> InheritsNames { get; }
        public IReadOnlyCollection<NameReference> BaseOfNames { get; }

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> OwnedNodes => this.BaseOfNames;

        public TemplateParameter(int index,
            string name,
            VarianceMode variance,
            EntityModifier constraintModifier,
            IEnumerable<NameReference> inherits,
            IEnumerable<NameReference> baseOf)
            : base()
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Index = index;
            this.Name = name;
            this.Variance = variance;
            this.ConstraintModifier = constraintModifier;
            this.InheritsNames = (inherits ?? Enumerable.Empty<NameReference>()).StoreReadOnly();
            this.BaseOfNames = (baseOf ?? Enumerable.Empty<NameReference>()).StoreReadOnly();
            this.AssociatedType = TypeDefinition.CreateTypeParameter(this.ConstraintModifier, NameDefinition.Create(Name), 
                InheritsNames, this);
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
