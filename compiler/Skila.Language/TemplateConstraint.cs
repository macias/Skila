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
    public sealed class TemplateConstraint : Node, IVerificable
    {
        public NameReference Name { get; }
        // for example "const" means the type argument has to be immutable
        public EntityModifier Modifier { get; }
        public IReadOnlyCollection<NameReference> InheritsNames { get; }
        public IReadOnlyCollection<NameReference> BaseOfNames { get; }
        public IReadOnlyCollection<FunctionDefinition> Functions { get; }

        // do not report parent typenames as owned, because they will be reused 
        // as parent typenames in associated type definition
        public override IEnumerable<INode> OwnedNodes => this.BaseOfNames.Concat(this.Name);

        public TemplateConstraint(
            NameReference name,
            EntityModifier constraintModifier,
            IEnumerable<FunctionDefinition> functions,
            IEnumerable<NameReference> inherits,
            IEnumerable<NameReference> baseOf)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Name = name;
            this.Modifier = constraintModifier ?? EntityModifier.None;
            this.Functions = (functions ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.InheritsNames = (inherits ?? Enumerable.Empty<NameReference>()).StoreReadOnly();
            this.BaseOfNames = (baseOf ?? Enumerable.Empty<NameReference>()).StoreReadOnly();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public void Verify(ComputationContext ctx)
        {
            foreach (NameReference base_of in this.BaseOfNames)
                // we allow slicing because we just need if the hierarchy is not reversed, not to pass actual values
                if (this.InheritsNames.Any(it
                    => it.Evaluation.Components.MatchesTarget(ctx, base_of.Evaluation.Components, allowSlicing: true) == TypeMatch.Pass))
                    ctx.AddError(ErrorCode.ConstraintConflictingTypeHierarchy, base_of);
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
