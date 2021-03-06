﻿using System;
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
    public sealed class TemplateConstraint : OwnedNode, IValidable
    {
        public static TemplateConstraint Create(
            NameReference name,
            EntityModifier constraintModifier,
            IEnumerable<FunctionDefinition> hasFunctions,
            IEnumerable<NameReference> inherits,
            IEnumerable<NameReference> baseOf)
        {
            return new TemplateConstraint(name, constraintModifier, hasFunctions, inherits, baseOf);
        }
        public static TemplateConstraint Create(
            string name,
            EntityModifier constraintModifier,
            IEnumerable<FunctionDefinition> functions,
            IEnumerable<NameReference> inherits,
            IEnumerable<NameReference> baseOf)
        {
            return Create(NameReference.Create(name), constraintModifier, functions, inherits, baseOf);
        }

        public NameReference Name { get; }
        // for example "const" means the type argument has to be immutable
        public EntityModifier Modifier { get; }
        public IReadOnlyCollection<NameReference> InheritsNames { get; }
        public IReadOnlyCollection<NameReference> BaseOfNames { get; }
        public IReadOnlyCollection<FunctionDefinition> HasFunctions { get; }

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> ChildrenNodes => this.BaseOfNames.Concat(this.Name);

        private TemplateConstraint(
            NameReference name,
            EntityModifier constraintModifier,
            IEnumerable<FunctionDefinition> hasFunctions,
            IEnumerable<NameReference> inherits,
            IEnumerable<NameReference> baseOf)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Name = name;
            this.Modifier = constraintModifier ?? EntityModifier.None;
            this.HasFunctions = (hasFunctions ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.InheritsNames = (inherits ?? Enumerable.Empty<NameReference>()).StoreReadOnly();
            this.BaseOfNames = (baseOf ?? Enumerable.Empty<NameReference>()).StoreReadOnly();

            this.attachPostConstructor();
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public void Validate(ComputationContext ctx)
        {
            foreach (NameReference base_of in this.BaseOfNames)
                // we allow slicing because we just need if the hierarchy is not reversed, not to pass actual values
                if (this.InheritsNames.Any(it
                    =>
                {
                    TypeMatch match = it.Evaluation.Components.MatchesTarget(ctx, base_of.Evaluation.Components, 
                        TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true));
                    return match == TypeMatch.Same || match == TypeMatch.Substitute;
                }))
                    ctx.AddError(ErrorCode.ConstraintConflictingTypeHierarchy, base_of);

            // the left side: target match (i.e. template inner parameter type) -> template
            // the right side: this -> template parameter -> name definition -> template
            if (this.Name.Binding.Match.Instance.Target.Owner != this.Owner.Owner.Owner)
                ctx.AddError(ErrorCode.MisplacedConstraint,this);
        }

        public IEnumerable<EntityInstance> TranslateInherits(EntityInstance closedTemplate)
        {
            return InheritsNames.Select(it => it.Binding.Match.Instance)
                .Select(it => it.TranslateThrough(closedTemplate));
        }
        public IEnumerable<EntityInstance> TranslateBaseOf(EntityInstance closedTemplate)
        {
            return BaseOfNames.Select(it => it.Binding.Match.Instance)
                .Select(it => it.TranslateThrough(closedTemplate));
        }


    }
}
