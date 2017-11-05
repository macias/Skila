using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Expressions;
using Skila.Language.Flow;

namespace Skila.Language.Entities
{
    // please note that the setter is converted to the method where the value argument/parameters comes FIRST
    // to make sure there will be no problem in defining and passing this data
    // when user defines fancy parameters for indexer
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Property : Node, IEvaluable, IEntityVariable, IEntityScope
    {
        public static VariableDeclaration CreateAutoField(INameReference typeName, IExpression initValue, EntityModifier modifier = null)
        {
            return VariableDeclaration.CreateStatement(NameFactory.PropertyAutoField, typeName, initValue, modifier);
        }
        public static FunctionDefinition CreateAutoGetter(INameReference typeName)
        {
            return CreateProxyGetter(typeName, NameReference.Create(NameFactory.PropertyAutoField));
        }
        internal static FunctionDefinition CreateProxyGetter(INameReference typeName, IExpression passedExpression)
        {
            return FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.PropertyGetter),
                null, ExpressionReadMode.CannotBeRead, typeName,
                Block.CreateStatement(new[] {
                    Return.Create(passedExpression)
                }));
        }
        public static FunctionDefinition CreateAutoSetter(INameReference typeName)
        {
            return FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.PropertySetter),
                new[] { FunctionParameter.Create(NameFactory.PropertySetterParameter, typeName, Variadic.None, null, isNameRequired: false) },
                ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    Assignment.CreateStatement(NameReference.Create(NameFactory.PropertyAutoField),
                        NameReference.Create(NameFactory.PropertySetterParameter ))
                }));
        }

        public static Property Create(string name, INameReference typeName,
            IEnumerable<VariableDeclaration> fields,
            IEnumerable<FunctionDefinition> getters,
            IEnumerable<FunctionDefinition> setters,
            EntityModifier modifier = null)
        {
            return new Property(modifier, name, typeName, fields, getters, setters);
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

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, Getter, Setter }.Concat(Fields)
            .Where(it => it != null);
        public EntityModifier Modifier { get; }

        public IEntityInstance Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsComputed => this.Evaluation != null;

        private Property(EntityModifier modifier, string name, INameReference typeName,
            IEnumerable<VariableDeclaration> fields, IEnumerable<FunctionDefinition> getters, IEnumerable<FunctionDefinition> setters)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Name = NameDefinition.Create(name);
            this.TypeName = typeName;
            this.Fields = (fields ?? Enumerable.Empty<VariableDeclaration>()).StoreReadOnly();
            this.getters = (getters ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.setters = (setters ?? Enumerable.Empty<FunctionDefinition>()).StoreReadOnly();
            this.Modifier = (this.Setter == null ? EntityModifier.None: EntityModifier.Reassignable) | modifier;

            this.instanceOf = new Lazy<EntityInstance>(() => EntityInstance.RAW_CreateUnregistered(this, null));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Name} {this.TypeName}";
            return result;
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments)
        {
            return this.InstanceOf;
        }

        public void Validate( ComputationContext ctx)
        {
        }

        public bool IsReadingValueOfNode( IExpression node)
        {
            return false;
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.TypeName.Evaluated(ctx);

                foreach (FunctionDefinition dup_accessor in this.getters.Skip(1).Concat(this.setters.Skip(1)))
                    ctx.AddError(ErrorCode.PropertyMultipleAccessors, dup_accessor, this);

            }
        }

    }
}
