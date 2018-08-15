using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using Skila.Language.Printout;
using Skila.Language.Tools;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Alias : Expression, IMember, ILocalBindable
    {
        public static Alias Create(string name, INameReference replacement, EntityModifier modifier = null)
        {
            return new Alias( modifier, name, replacement);
        }

        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;
        private readonly EntityInstanceCache instancesCache;
        public NameDefinition Name { get; }
        public INameReference Replacement { get; }

        public bool IsMemberUsed { get; private set; }

        public override IEnumerable<INode> ChildrenNodes => new INode[] { Replacement, Modifier }
            .Where(it => it != null);

        public override ExecutionFlow Flow => ExecutionFlow.Empty;

        public EntityModifier Modifier { get; private set; }

        private Alias(EntityModifier modifier, string name, INameReference replacement)
            : base(ExpressionReadMode.CannotBeRead)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Modifier = (modifier ?? EntityModifier.None) | EntityModifier.Static;
            this.Name = NameDefinition.Create(name);
            this.Replacement = replacement;

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(null, TypeMutability.None,
                translation: TemplateTranslation.Create(this), lifetime: Lifetime.Timeless));

            this.attachPostConstructor();
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan(Name).Append(" = ").Append(this.Replacement);
        }
        public override bool AttachTo(IOwnedNode owner)
        {
            if (!base.AttachTo(owner))
                return false;

            if (owner is TypeContainerDefinition && !this.Modifier.IsAccessSet)
                this.SetModifier(this.Modifier | EntityModifier.Private);

            return true;
        }

        private void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            return this.instancesCache.GetInstance(arguments, overrideMutability, translation,lifetime);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation != null)
                return;

            IEntityInstance tn_eval = this.Replacement?.Evaluation?.Components;

            this.Evaluation = ctx.Env.UnitEvaluation;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);
        }

        public void SetIsMemberUsed()
        {
            this.IsMemberUsed = true;
        }

    }
}
