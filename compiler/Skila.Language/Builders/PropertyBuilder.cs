﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class PropertyBuilder : IBuilder<Property>
    {
        public static PropertyBuilder CreateAutoGetter(string name, NameReference typename,
                IExpression initValue = null)
        {
            return CreateAutoGetter(name, typename, out PropertyMembers dummy, initValue);
        }
        public static PropertyBuilder CreateAutoGetter(string name, NameReference typename,
            out PropertyMembers members, IExpression initValue = null)
        {
            PropertyBuilder builder = PropertyBuilder.Create(name, typename)
                .WithAutoField(initValue, EntityModifier.None, out VariableDeclaration field)
                .WithAutoGetter(out FunctionDefinition getter);

            members = new PropertyMembers() { Field = field, Getter = getter };

            return builder;
        }
        public static PropertyBuilder CreateAutoFull(string name, NameReference typename,
            out PropertyMembers members, IExpression initValue = null)
        {
            PropertyBuilder builder = PropertyBuilder.Create(name, typename)
                .WithAutoField(initValue, EntityModifier.Reassignable, out VariableDeclaration field)
                .WithAutoGetter(out FunctionDefinition getter)
                .WithAutoSetter(out FunctionDefinition setter);

            members = new PropertyMembers() { Field = field, Getter = getter, Setter = setter };

            return builder;
        }
        public static PropertyBuilder CreateAutoFull(string name, NameReference typename, IExpression initValue = null)
        {
            return CreateAutoFull(name, typename, out PropertyMembers dummy, initValue);
        }
        public static PropertyBuilder Create(string name, NameReference typename, EntityModifier modifier = null)
        {
            return new PropertyBuilder(name, typename, modifier);
        }
        public static PropertyBuilder CreateIndexer(NameReference valueTypeName, EntityModifier modifier = null)
        {
            return new PropertyBuilder(null, valueTypeName, modifier);
        }

        private readonly List<VariableDeclaration> fields;
        private readonly List<FunctionDefinition> getters;
        private readonly List<FunctionDefinition> setters;
        private Property build;
        private readonly string name;
        public NameReference ValueTypeName { get; }
        private EntityModifier modifier;
        public IEnumerable<FunctionParameter> Params { get; private set; }

        private PropertyBuilder(string name, NameReference valueTypeName, EntityModifier modifier)
        {
            this.name = name;
            this.ValueTypeName = valueTypeName;
            this.modifier = modifier;

            this.fields = new List<VariableDeclaration>();
            this.getters = new List<FunctionDefinition>();
            this.setters = new List<FunctionDefinition>();
        }

        public Property Build()
        {
            if (build == null)
            {
                if (this.name == null)
                    build = Property.CreateIndexer(this.ValueTypeName, fields, getters, setters, modifier);
                else
                    build = Property.Create(this.name, this.ValueTypeName, fields, getters, setters, modifier);
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

            field = Property.CreateAutoField(this.ValueTypeName, initValue, modifier);
            this.fields.Add(field);

            return this;
        }
        public PropertyBuilder With(VariableDeclaration field)
        {
            if (build != null)
                throw new Exception();

            this.fields.Add(field);

            return this;
        }

        public PropertyBuilder WithAutoField(IExpression initValue, EntityModifier modifier)
        {
            return WithAutoField(initValue, modifier, out VariableDeclaration field);
        }
        public PropertyBuilder WithAutoGetter(out FunctionDefinition getter, EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();

            getter = Property.CreateAutoGetter(this.ValueTypeName, modifier);
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

            getter = Property.CreateGetter(this.ValueTypeName, body, modifier);
            this.getters.Add(getter);

            return this;
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

            setter = Property.CreateAutoSetter(this.ValueTypeName, modifier);
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
