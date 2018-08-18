using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class PropertyBuilder : IBuilder<Property>
    {
        public static PropertyBuilder CreateAutoGetter(IOptions options, string name, NameReference typename,
                IExpression initValue = null)
        {
            return CreateAutoGetter(options, name, typename, out PropertyMembers dummy, initValue);
        }
        public static PropertyBuilder CreateAutoGetter(IOptions options, string name, NameReference typename,
            out PropertyMembers members, IExpression initValue = null)
        {
            PropertyBuilder builder = PropertyBuilder.Create(options, name, () => typename)
                .WithAutoField(initValue, EntityModifier.None, out VariableDeclaration field)
                .WithAutoGetter(out FunctionDefinition getter);

            members = new PropertyMembers() { Field = field, Getter = getter };

            return builder;
        }
        public static PropertyBuilder CreateAutoFull(IOptions options, string name, NameReference typename,
            out PropertyMembers members, IExpression initValue = null)
        {
            PropertyBuilder builder = PropertyBuilder.Create(options, name, () => typename)
                .WithAutoField(initValue, options.ReassignableModifier(), out VariableDeclaration field)
                .WithAutoGetter(out FunctionDefinition getter)
                .WithAutoSetter(out FunctionDefinition setter);

            members = new PropertyMembers() { Field = field, Getter = getter, Setter = setter };

            return builder;
        }
        public static PropertyBuilder CreateAutoFull(IOptions options, string name, NameReference typename, IExpression initValue = null)
        {
            return CreateAutoFull(options, name, typename, out PropertyMembers dummy, initValue);
        }
        public static PropertyBuilder Create(IOptions options, string name, Func<NameReference> typename, EntityModifier modifier = null)
        {
            return new PropertyBuilder(options, name, typename, modifier, referential: false);
        }
        public static PropertyBuilder CreateReferential(IOptions options, string name, Func<NameReference> typename,
            EntityModifier modifier = null)
        {
            return new PropertyBuilder(options, name, typename, modifier, referential: true);
        }
        public static PropertyBuilder CreateIndexer(IOptions options, NameReference valueTypeName, EntityModifier modifier = null)
        {
            return new PropertyBuilder(options, null, () => valueTypeName, modifier, referential: false);
        }
        public static PropertyBuilder CreateIndexer(IOptions options, Func<NameReference> valueTypeName, EntityModifier modifier = null)
        {
            return new PropertyBuilder(options, null, valueTypeName, modifier, referential: false);
        }

        private readonly List<VariableDeclaration> fields;
        private VariableDeclaration autoField;
        private readonly List<FunctionDefinition> getters;
        private readonly List<FunctionDefinition> setters;
        private Property build;
        // true = setter and getter works through reference (and property is seen as reference-of type)
        private readonly bool referential;
        private readonly IOptions options;
        private readonly string name;
        public readonly Func<NameReference> valueTypeNameFactory;
        public NameReference ValueTypeName => this.valueTypeNameFactory();
        private EntityModifier modifier;
        public IEnumerable<FunctionParameter> Params { get; private set; }

        private PropertyBuilder(IOptions options, string name, Func<NameReference> valueTypeName, EntityModifier modifier, bool referential)
        {
            this.referential = referential;
            this.options = options;
            this.name = name;
            this.valueTypeNameFactory = valueTypeName;
            this.modifier = modifier;

            this.fields = new List<VariableDeclaration>();
            this.getters = new List<FunctionDefinition>();
            this.setters = new List<FunctionDefinition>();
        }

        public Property Build()
        {
            if (build == null)
            {
                IEnumerable<VariableDeclaration> prop_fields = fields.Concat(autoField).Where(it => it != null);

                if (this.name == null)
                    build = Property.CreateIndexer(options, this.ValueTypeName, prop_fields, getters, setters, modifier);
                else
                {
                    build = Property.Create(options, this.name, this.getTransferValueTypeName(), prop_fields, getters, setters, modifier);
                }
            }

            return build;
        }

        public PropertyBuilder Parameters(params FunctionParameter[] parameters)
        {
            if (this.Params != null || this.build != null)
                throw new InvalidOperationException();

            this.Params = parameters;
            return this;
        }

        public PropertyBuilder SetModifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }

        public PropertyBuilder With(PropertyMemberBuilder builder)
        {
            return With(builder, out IMember member);
        }

        public PropertyBuilder With(PropertyMemberBuilder builder, out IMember member)
        {
            if (build != null)
                throw new Exception();

            member = builder.Build(this);
            if (member is VariableDeclaration decl)
                this.fields.Add(decl);
            else if (member is FunctionDefinition func)
            {
                if (func.Name.Name == NameFactory.PropertyGetter)
                    this.getters.Add(func);
                else if (func.Name.Name == NameFactory.PropertySetter)
                    this.setters.Add(func);
                else
                    throw new Exception();
            }
            else
                throw new Exception();

            return this;
        }

        public PropertyBuilder WithAutoField(IExpression initValue, EntityModifier modifier, out VariableDeclaration field)
        {
            if (build != null)
                throw new Exception();
            if (this.autoField != null)
                throw new ArgumentException();

            field = Property.CreateAutoField(AutoName.Instance.CreateNew(NameFactory.PropertyAutoField),
                this.ValueTypeName, initValue, modifier);
            this.autoField = field;

            return this;
        }
        public PropertyBuilder With(VariableDeclaration field)
        {
            if (build != null)
                throw new Exception();

            this.fields.Add(field);

            return this;
        }

        public PropertyBuilder WithAutoField(IExpression initValue, EntityModifier modifier = null)
        {
            return WithAutoField(initValue, modifier, out VariableDeclaration field);
        }
        public PropertyBuilder WithAutoGetter(out FunctionDefinition getter, EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();
            if (this.autoField == null)
                throw new InvalidOperationException();

            getter = Property.CreateAutoGetter(this.autoField.Name.Name, this.getTransferValueTypeName(), modifier);
            this.getters.Add(getter);

            return this;
        }

        public PropertyBuilder WithAutoGetter(EntityModifier modifier = null)
        {
            return WithAutoGetter(out FunctionDefinition getter, modifier);
        }

        public PropertyBuilder WithGetter(Block body, out FunctionDefinition getter, EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();

            getter = Property.CreateGetter(getTransferValueTypeName(), body, modifier);
            this.getters.Add(getter);

            return this;
        }

        private NameReference getTransferValueTypeName()
        {
            if (this.referential)
                return NameFactory.ReferenceNameReference(this.ValueTypeName);
            else
                return this.ValueTypeName;
        }

        public PropertyBuilder WithSetter(Block body, out FunctionDefinition setter, EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();

            setter = Property.CreateSetter(this.ValueTypeName, body, modifier);
            this.setters.Add(setter);

            return this;
        }
        public PropertyBuilder WithGetter(Block body, EntityModifier modifier = null)
        {
            return WithGetter(body, out FunctionDefinition dummy, modifier);
        }

        public PropertyBuilder WithSetter(Block body, EntityModifier modifier = null)
        {
            return WithSetter(body, out FunctionDefinition dummy, modifier);
        }
        public PropertyBuilder WithAutoSetter(out FunctionDefinition setter, EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();
            if (this.autoField == null)
                throw new InvalidOperationException();

            setter = Property.CreateAutoSetter(this.autoField.Name.Name, this.getTransferValueTypeName(), modifier);
            this.setters.Add(setter);

            return this;
        }

        public PropertyBuilder WithAutoSetter(EntityModifier modifier = null)
        {
            return WithAutoSetter(out FunctionDefinition setter, modifier);
        }

        public static implicit operator Property(PropertyBuilder @this)
        {
            return @this.Build();
        }
    }
}
