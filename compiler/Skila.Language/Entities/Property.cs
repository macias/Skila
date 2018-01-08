using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Builders;

namespace Skila.Language.Entities
{
    // please note that the setter is converted to the method where the value argument/parameters comes FIRST
    // to make sure there will be no problem in defining and passing this data
    // when user defines fancy parameters for indexer
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Property : Node, IEvaluable, IEntityVariable, IEntityScope, IMember, ISurfable
    {
        public static FunctionDefinition CreateIndexerGetter(INameReference propertyTypeName,
            IEnumerable<FunctionParameter> parameters, Block body)
        {
            return CreateIndexerGetter(propertyTypeName, parameters, EntityModifier.None, body);
        }
        public static FunctionDefinition CreateIndexerGetter(INameReference propertyTypeName,
            IEnumerable<FunctionParameter> parameters, EntityModifier modifier, Block body)
        {
            return FunctionBuilder.Create(NameFactory.PropertyGetter,
                ExpressionReadMode.ReadRequired, propertyTypeName,
                body)
                .Modifier(modifier)
                .Parameters(parameters);
        }
        public static FunctionDefinition CreateIndexerSetter(INameReference propertyTypeName,
            IEnumerable<FunctionParameter> parameters, Block body)
        {
            return CreateIndexerSetter(propertyTypeName, parameters, EntityModifier.None, body);
        }
        public static FunctionDefinition CreateIndexerSetter(INameReference propertyTypeName,
            IEnumerable<FunctionParameter> parameters, EntityModifier modifier, Block body)
        {
            return FunctionBuilder.Create(NameFactory.PropertySetter,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                body)
                .Modifier(modifier | EntityModifier.Mutable)
                    .Parameters(parameters.Concat(FunctionParameter.Create(NameFactory.PropertySetterValueParameter,
                        // we add "value" parameter at the end so the name has to be required, 
                        // because we don't know what comes first
                        propertyTypeName, Variadic.None, null, isNameRequired: true,
                        usageMode: modifier.HasNative ? ExpressionReadMode.CannotBeRead : ExpressionReadMode.ReadRequired)));
        }
        public static VariableDeclaration CreateAutoField(INameReference typeName, IExpression initValue, EntityModifier modifier = null)
        {
            return VariableDeclaration.CreateStatement(NameFactory.PropertyAutoField, typeName, initValue, modifier);
        }
        public static FunctionDefinition CreateAutoGetter(INameReference typeName, EntityModifier modifier = null)
        {
            return CreateGetter(typeName,
                Block.CreateStatement(Return.Create(
                    NameReference.Create(NameFactory.ThisVariableName, NameFactory.PropertyAutoField))),
                modifier);
        }
        internal static FunctionDefinition CreateGetter(INameReference typeName, Block body, EntityModifier modifier = null)
        {
            return FunctionDefinition.CreateFunction(modifier, NameDefinition.Create(NameFactory.PropertyGetter),
                null,
                null,
                ExpressionReadMode.ReadRequired,
                typeName,
                body);
        }
        public static FunctionDefinition CreateSetter(INameReference typeName, Block body, EntityModifier modifier = null)
        {
            return FunctionDefinition.CreateFunction(modifier | EntityModifier.Mutable, NameDefinition.Create(NameFactory.PropertySetter),
                null,
                new[] { FunctionParameter.Create(NameFactory.PropertySetterValueParameter, typeName) },
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                body);
        }
        public static FunctionDefinition CreateAutoSetter(INameReference typeName)
        {
            return FunctionDefinition.CreateFunction(EntityModifier.Mutable, NameDefinition.Create(NameFactory.PropertySetter),
                null,
                new[] { FunctionParameter.Create(NameFactory.PropertySetterValueParameter, typeName) },
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new[] {
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, NameFactory.PropertyAutoField),
                        NameReference.Create(NameFactory.PropertySetterValueParameter ))
                }));
        }

        public static Property Create(string name,
            INameReference typeName,
            IEnumerable<VariableDeclaration> fields,
            IEnumerable<FunctionDefinition> getters,
            IEnumerable<FunctionDefinition> setters,
            EntityModifier modifier = null)
        {
            return new Property(modifier, name, null, typeName, fields, getters, setters);
        }

        public static Property CreateIndexer(
            INameReference typeName,
            IEnumerable<VariableDeclaration> fields,
            IEnumerable<FunctionDefinition> getters,
            IEnumerable<FunctionDefinition> setters,
            EntityModifier modifier = null)
        {
            return Create(NameFactory.PropertyIndexerName, typeName, fields, getters, setters, modifier);
        }

        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;
        private readonly EntityInstanceCache instancesCache;
        public NameDefinition Name { get; }
        public INameReference TypeName { get; }

        private readonly IReadOnlyCollection<FunctionDefinition> getters;
        private readonly IReadOnlyCollection<FunctionDefinition> setters;
        public IReadOnlyCollection<VariableDeclaration> Fields { get; }

        public IEnumerable<FunctionDefinition> Accessors => new[] { this.Getter, this.Setter }.Where(it => it != null);

        public FunctionDefinition Getter { get { return getters.FirstOrDefault(); } }
        public FunctionDefinition Setter { get { return setters.FirstOrDefault(); } }

        public IEnumerable<EntityInstance> AvailableEntities => this.NestedEntityInstances();
        public bool IsSurfed { get; set; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, Getter, Setter, Modifier }
            .Concat(Fields)
            .Where(it => it != null);
        public EntityModifier Modifier { get; private set; }

        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsComputed => this.Evaluation != null;

        public bool IsIndexer => this.Name.Name == NameFactory.PropertyIndexerName;

        public bool IsMemberUsed { get; private set; }

        private Property(EntityModifier modifier, string name, IEnumerable<FunctionParameter> parameters, INameReference typeName,
            IEnumerable<VariableDeclaration> fields, IEnumerable<FunctionDefinition> getters, IEnumerable<FunctionDefinition> setters)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Name = NameDefinition.Create(name);
            this.TypeName = typeName;
            this.Fields = (fields ?? Enumerable.Empty<VariableDeclaration>()).StoreReadOnly();
            this.getters = (getters ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.setters = (setters ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.Modifier = (this.Setter == null ? EntityModifier.None : EntityModifier.Reassignable) | modifier;

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(null, MutabilityFlag.ConstAsSource, null));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Name} {this.TypeName}";
            return result;
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, MutabilityFlag overrideMutability, TemplateTranslation translation)
        {
            return this.instancesCache.GetInstance(arguments, overrideMutability, translation);
        }

        public void Validate(ComputationContext ctx)
        {
            IEntityScopeExtension.Validate(this, ctx);
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.TypeName.Evaluation;

                foreach (FunctionDefinition dup_accessor in this.getters.Skip(1).Concat(this.setters.Skip(1)))
                    ctx.AddError(ErrorCode.PropertyMultipleAccessors, dup_accessor, this);

            }
        }

        public override bool AttachTo(INode parent)
        {
            if (!base.AttachTo(parent))
                return false;

            if (!this.Modifier.IsAccessSet)
            {
                if (parent is TypeContainerDefinition)
                    this.SetModifier(this.Modifier | EntityModifier.Public);
            }

            // we need to notify accessors about attachment to type, so those methods could create correct "this" parameter
            this.Getter?.AttachTo(this);
            this.Setter?.AttachTo(this);

            return true;
        }

        private void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public void SetIsMemberUsed()
        {
            this.IsMemberUsed = true;
        }

        public void Surf(ComputationContext ctx)
        {
            ;
        }

    }
}
