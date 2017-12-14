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
    public sealed class Property : Node, IEvaluable, IEntityVariable, IEntityScope, IMember
    {
        public static FunctionDefinition CreateIndexerGetter(INameReference propertyTypeName, IEnumerable<FunctionParameter> parameters, params IExpression[] instructions)
        {
            return FunctionBuilder.Create(NameFactory.PropertyGetter,
                ExpressionReadMode.ReadRequired, propertyTypeName, Block.CreateStatement(instructions))
                .Parameters(parameters);
        }
        public static FunctionDefinition CreateIndexerSetter(INameReference propertyTypeName, IEnumerable<FunctionParameter> parameters, params IExpression[] instructions)
        {
            return FunctionBuilder.Create(NameFactory.PropertySetter,
                ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), Block.CreateStatement(instructions))
                    .Parameters(parameters.Concat(FunctionParameter.Create(NameFactory.PropertySetterValueParameter, 
                        // we add "value" parameter at the end so the name has to be required, 
                        // because we don't know what comes first
                        propertyTypeName, Variadic.None, null, isNameRequired: true)));
        }
        public static VariableDeclaration CreateAutoField(INameReference typeName, IExpression initValue, EntityModifier modifier = null)
        {
            return VariableDeclaration.CreateStatement(NameFactory.PropertyAutoField, typeName, initValue, modifier);
        }
        public static FunctionDefinition CreateAutoGetter(INameReference typeName)
        {
            return CreateProxyGetter(typeName, NameReference.Create(NameFactory.ThisVariableName, NameFactory.PropertyAutoField));
        }
        internal static FunctionDefinition CreateProxyGetter(INameReference typeName, IExpression passedExpression)
        {
            return FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.PropertyGetter),
                null,
                null, ExpressionReadMode.CannotBeRead, typeName,
                Block.CreateStatement(new[] {
                    Return.Create(passedExpression)
                }));
        }
        public static FunctionDefinition CreateAutoSetter(INameReference typeName)
        {
            return FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.PropertySetter),
                null,
                new[] { FunctionParameter.Create(NameFactory.PropertySetterValueParameter, typeName) },
                ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(),
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

        private readonly Lazy<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;
        public NameDefinition Name { get; }
        public INameReference TypeName { get; }

        private readonly IReadOnlyCollection<FunctionDefinition> getters;
        private readonly IReadOnlyCollection<FunctionDefinition> setters;
        public IReadOnlyCollection<VariableDeclaration> Fields { get; }

        public FunctionDefinition Getter { get { return getters.FirstOrDefault(); } }
        public FunctionDefinition Setter { get { return setters.FirstOrDefault(); } }

        public IEnumerable<IEntity> AvailableEntities => this.NestedEntities();

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, Getter, Setter, Modifier }
            .Concat(Fields)
            .Where(it => it != null);
        public EntityModifier Modifier { get; }

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

            this.instanceOf = new Lazy<EntityInstance>(() => EntityInstance.RAW_CreateUnregistered(this, EntityInstanceSignature.None));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Name} {this.TypeName}";
            return result;
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments, bool overrideMutability)
        {
            return this.InstanceOf;
        }

        public void Validate(ComputationContext ctx)
        {
            IEntityScopeExtension.Validate(this,ctx);
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

            // we need to notify accessors about attachment to type, so those methods could create correct "this" parameter
            this.Getter?.AttachTo(this);
            this.Setter?.AttachTo(this);

            return true;
        }

        public void SetIsMemberUsed()
        {
            this.IsMemberUsed = true;
        }

    }
}
