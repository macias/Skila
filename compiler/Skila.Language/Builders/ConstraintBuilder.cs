using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class ConstraintBuilder : IBuilder<TemplateConstraint>
    {
        public static ConstraintBuilder Create(
                   string name)
        {
            return new ConstraintBuilder(NameReference.Create(name));
        }

        private readonly NameReference name;
        private IEnumerable<FunctionDefinition> hasConstraints;

        private TemplateConstraint build;
        private EntityModifier modifier;

        private IEnumerable<NameReference> inherits;
        private IEnumerable<NameReference> baseOf;

        public ConstraintBuilder(NameReference name)
        {
            this.name = name;
        }

        public ConstraintBuilder SetModifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }
        public ConstraintBuilder Has(params FunctionDefinition[] functions)
        {
            if (this.hasConstraints != null || this.build != null)
                throw new InvalidOperationException();

            this.hasConstraints = functions;
            return this;
        }
        public ConstraintBuilder Inherits(params string[] inherits)
        {
            return Inherits(inherits.Select(it => NameReference.Create(it)).ToArray());
        }
        public ConstraintBuilder Inherits(params NameReference[] inherits)
        {
            if (this.inherits != null || this.build != null)
                throw new InvalidOperationException();

            this.inherits = inherits;
            return this;
        }
        public ConstraintBuilder BaseOf(params string[] baseOf)
        {
            return BaseOf(baseOf.Select(it => NameReference.Create(it)).ToArray());
        }
        public ConstraintBuilder BaseOf(params NameReference[] baseOf)
        {
            if (this.baseOf != null || this.build != null)
                throw new InvalidOperationException();

            this.baseOf = baseOf;
            return this;
        }


        public TemplateConstraint Build()
        {
            if (build == null)
                build = TemplateConstraint.Create(name,modifier,hasConstraints,inherits,baseOf);
            return build;
        }
        public static implicit operator TemplateConstraint(ConstraintBuilder @this)
        {
            return @this.Build();
        }
    }
}
