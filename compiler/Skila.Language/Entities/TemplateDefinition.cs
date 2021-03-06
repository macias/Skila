﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Builders;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Printout;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class TemplateDefinition : OwnedNode, IEntity, IEntityScope, ISurfable
    {
        private readonly HashSet<INode> ownedNodes;
        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;

        public NameDefinition Name { get; }

        public IEnumerable<FunctionDefinition> NestedFunctions => this.NestedTemplates.WhereType<FunctionDefinition>();
        public IEnumerable<FunctionDefinition> AllNestedFunctions => this.NestedFunctions
            .Concat(this.NestedProperties.SelectMany(it => it.Accessors));
        public IEnumerable<VariableDeclaration> NestedFields => this.ownedNodes.WhereType<VariableDeclaration>();
        // directly nested fields + property fields
        public IEnumerable<VariableDeclaration> AllNestedFields => this.NestedFields
            .Concat(this.NestedProperties.SelectMany(it => it.Fields));
        public IEnumerable<Property> NestedProperties => this.ownedNodes.WhereType<Property>();
        public IEnumerable<Namespace> NestedNamespaces => this.NestedTemplates.WhereType<Namespace>();
        public IEnumerable<TypeContainerDefinition> NestedTypeContainers => this.NestedTemplates.WhereType<TypeContainerDefinition>();
        public IEnumerable<TemplateDefinition> NestedTemplates => this.ownedNodes.WhereType<TemplateDefinition>();

        public override IEnumerable<INode> ChildrenNodes => this.ownedNodes
            .Concat(this.Name)
            .Concat(Conditionals)
            .Concat(Modifier)
            .Where(it => it != null)
            .Concat(Includes);

        private readonly EntityInstanceCache instancesCache;

        public bool IsComputed { get; protected set; }
        private readonly Lazy<EvaluationInfo> evaluation;
        public EvaluationInfo Evaluation => this.evaluation.Value;
        public ValidationData Validation { get; set; }
        public IEnumerable<NameReference> Includes { get; }
        public EntityModifier Modifier { get; protected set; }
        public IEnumerable<TemplateConstraint> Constraints { get; }
        // constraints that sets the availability of the entire template
        // for example "Array<T>" can have method "copy" present with conditional on the method (not entire type)
        // that T has method "copy" itself, otherwise the method "Array.copy" is not available
        public IEnumerable<TemplateConstraint> Conditionals { get; }

        public abstract ScopeTable AvailableEntities { get; }

        public bool IsSurfed { get; set; }

        // used to protect ourselves against adding extra nodes after the object is built
        // used in functions and types, not namespaces
        protected bool constructionCompleted;

        protected TemplateDefinition(EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints, IEnumerable<NameReference> includes) : base()
        {
            modifier = modifier ?? EntityModifier.None;
            if (modifier.HasEnum)
                modifier |= EntityModifier.Const;

            this.Includes = (includes ?? Enumerable.Empty<NameReference>()).StoreReadOnly();
            this.Modifier = modifier;
            this.Constraints = (constraints ?? Enumerable.Empty<TemplateConstraint>()).StoreReadOnly();
            this.ownedNodes = new HashSet<INode>(ReferenceEqualityComparer<INode>.Instance);
            this.Name = name;

            {
                var set = this.Constraints.ToHashSet();
                foreach (TemplateParameter param in name.Parameters)
                {
                    TemplateConstraint constraint = this.Constraints.SingleOrDefault(it => it.Name.Name == param.Name);
                    param.SetConstraint(constraint);
                    set.Remove(constraint);

                    this.AddNode(param.AssociatedType);
                }

                this.Conditionals = set;
            }

            this.instancesCache = new EntityInstanceCache(this,
                () => this.GetInstance(
                    TypeMutability.None,
                    TemplateTranslation.Create(this, this.Name.Parameters.Select(it => it.InstanceOf)),
                    Lifetime.Timeless));

            this.evaluation = new Lazy<EvaluationInfo>(() => Environment.JokerEval);
        }

        public T AddBuilder<T>(IBuilder<T> builder)
            where T : IOwnedNode
        {
            return this.AddNode(builder.Build());
        }
        public T AddNode<T>(T elem)
            // todo: set constraint as IEntity and move towards meaning "members"
            where T : INode
        {
            if (this.constructionCompleted)
                throw new Exception("Internal error");

            if (!ownedNodes.Add(elem))
                throw new ArgumentException("Element was already added.");
            if (elem is IOwnedNode ownable_node)
                ownable_node.AttachTo(this);
            return elem;
        }

        public bool ContainsElement(TemplateDefinition elem)
        {
            return this.ownedNodes.Contains(elem);
        }

        public EntityInstance GetInstance(TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            return this.instancesCache.GetInstance( overrideMutability, translation, lifetime);
        }

        public override string ToString()
        {
            return this.Printout().ToString();
        }

        public virtual ICode Printout()
        {
            return this.Name.Printout();
        }

        public virtual void Validate(ComputationContext ctx)
        {
        }

        public virtual void Evaluate(ComputationContext ctx)
        {

        }


        public abstract void Surf(ComputationContext ctx);
    }
}