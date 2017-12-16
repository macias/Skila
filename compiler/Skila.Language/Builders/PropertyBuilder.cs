﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class PropertyBuilder : IBuilder<Property>
    {
        public static PropertyBuilder Create(string name, NameReference typename,EntityModifier modifier = null)
        {
            return new PropertyBuilder(name, typename,modifier);
        }
        public static PropertyBuilder CreateIndexer(NameReference typename,EntityModifier modifier = null)
        {
            return new PropertyBuilder(null, typename,modifier);
        }

        private readonly List<VariableDeclaration> fields;
        private readonly List<FunctionDefinition> getters;
        private readonly List<FunctionDefinition> setters;
        private Property build;
        private readonly string name;
        private readonly NameReference typename;
        private readonly EntityModifier modifier;

        private PropertyBuilder(string name, NameReference typename,EntityModifier modifier)
        {
            this.name = name;
            this.typename = typename;
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
                    build = Property.CreateIndexer(this.typename, fields, getters, setters,modifier);
                else
                    build = Property.Create(this.name, this.typename, fields, getters, setters,modifier);
            }

            return build;
        }

        public PropertyBuilder With(PropertyMemberBuilder builder)
        {
            if (build != null)
                throw new Exception();

            IMember member = builder.Build(this.typename);
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

        public PropertyBuilder WithAutoField(IExpression initValue, EntityModifier modifier)
        {
            if (build != null)
                throw new Exception();

            this.fields.Add(Property.CreateAutoField(this.typename, initValue, modifier));

            return this;
        }

        public PropertyBuilder WithAutoGetter(EntityModifier modifier = null)
        {
            if (build != null)
                throw new Exception();

            this.getters.Add(Property.CreateAutoGetter(this.typename));

            return this;
        }

        public PropertyBuilder WithAutoSetter()
        {
            if (build != null)
                throw new Exception();

            this.setters.Add(Property.CreateAutoSetter(this.typename));

            return this;
        }
    }
}