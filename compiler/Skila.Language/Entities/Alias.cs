﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Alias : Expression, IMember, ILocalBindable
    {
        public static Alias Create(string name, INameReference replacement, EntityModifier modifier = null)
        {
            return new Alias(modifier, name, replacement);
        }

        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;
        private readonly EntityInstanceCache instancesCache;
        public NameDefinition Name { get; }
        public INameReference Replacement { get; }

        public bool IsMemberUsed { get; private set; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Replacement, Modifier }
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

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(null, MutabilityFlag.ConstAsSource,
                translation: TemplateTranslation.Create(this)));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Name} = {this.Replacement}";
            return result;
        }

        public override bool AttachTo(INode owner)
        {
            if (this.DebugId.Id == 123498)
            {
                ;
            }
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

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, MutabilityFlag overrideMutability,
            TemplateTranslation translation)
        {
            return this.instancesCache.GetInstance(arguments, overrideMutability, translation);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation != null)
                return;

            if (this.DebugId.Id == 249)
            {
                ;
            }

            IEntityInstance tn_eval = this.Replacement?.Evaluation?.Components;

            this.Evaluation = ctx.Env.UnitEvaluation;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);        
        }

        public void SetIsMemberUsed()
        {
            if (this.DebugId.Id == 2965)
            {
                ;
            }
            this.IsMemberUsed = true;
        }

    }
}