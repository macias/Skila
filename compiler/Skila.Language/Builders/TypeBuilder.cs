using System;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Expressions;
using Skila.Language.Flow;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeBuilder : IBuilder<TypeDefinition>
    {
        public static TypeBuilder Create(NameDefinition name)
        {
            return new TypeBuilder(name);
        }
        public static TypeBuilder Create(string name, params string[] typeParameters)
        {
            var buff = TemplateParametersBuffer.Create();
            typeParameters.ForEach(it => buff.Add(it, VarianceMode.None));
            return new TypeBuilder(NameDefinition.Create(name, buff.Values));
        }
        public static TypeBuilder CreateEnum(string name)
        {
            TypeBuilder builder = new TypeBuilder(NameDefinition.Create(name));
            builder = builder
                .Modifier(EntityModifier.Enum)
                .Parents(NameFactory.ObjectTypeReference(), NameFactory.EquatableTypeReference())
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    new[] { FunctionParameter.Create(NameFactory.EnumConstructorParameter, NameFactory.IntTypeReference(), 
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp",builder.CreateTypeNameReference(), ExpressionReadMode.CannotBeRead)))
                    ;

            return builder;
        }
        public static TypeBuilder CreateInterface(string name, EntityModifier modifier = null)
        {
            return CreateInterface(NameDefinition.Create(name), modifier);
        }
        public static TypeBuilder CreateInterface(NameDefinition name, EntityModifier modifier = null)
        {
            return new TypeBuilder(name).Modifier(EntityModifier.Interface | modifier);
        }


        private readonly NameDefinition name;
        private readonly List<INode> features;
        private NameReference[] parents;
        private EntityModifier modifier;
        private TypeDefinition build;
        private bool allowSlicing;
        private IEnumerable<TemplateConstraint> constraints;

        private TypeBuilder(NameDefinition name)
        {
            this.name = name;
            this.features = new List<INode>();
        }
        public TypeBuilder Parents(params NameReference[] parents)
        {
            if (this.parents != null || this.build != null)
                throw new InvalidOperationException();

            this.parents = parents;
            return this;
        }
        public NameReference CreateTypeNameReference()
        {
            return this.name.CreateNameReference(prefix: null);
        }
        public TypeBuilder Constraints(params TemplateConstraint[] constraints)
        {
            if (this.constraints != null || this.build != null)
                throw new InvalidOperationException();

            this.constraints = constraints;
            return this;
        }
        public TypeBuilder Parents(params string[] parents)
        {
            return this.Parents(parents.Select(it => NameReference.Create(it)).ToArray());
        }

        public TypeBuilder With(EnumCaseBuilder enumBuilder)
        {
            if (!this.modifier.HasEnum)
                throw new InvalidOperationException();
            return this.With(enumBuilder.Build(this));
        }
        public TypeBuilder With(params INode[] nodes)
        {
            this.features.AddRange(nodes);
            return this;
        }
        public TypeBuilder With(IEnumerable<INode> nodes)
        {
            this.features.AddRange(nodes);
            return this;
        }
        public TypeBuilder With(IBuilder<INode> builder)
        {
            return With(builder.Build());
        }

        public TypeBuilder Slicing(bool allow)
        {
            this.allowSlicing = allow;
            return this;
        }

        public TypeBuilder Modifier(EntityModifier modifier)
        {
            if (this.modifier != null || this.build != null)
                throw new InvalidOperationException();

            this.modifier = modifier;
            return this;
        }

        public TypeDefinition Build()
        {
            if (build == null)
                build = TypeDefinition.Create(this.modifier,
                    this.name,
                    this.constraints,
                    allowSlicing,
                    this.parents,
                    // put fields first so when function refers to variable it is already evaluated (midly hackerish)
                    // this avoids clearing/restoring local names registry of the evaluated function
                    features.OrderBy(it => it is VariableDeclaration ? 0 : 1));
            return build;
        }
        public static implicit operator TypeDefinition(TypeBuilder @this)
        {
            return @this.Build();
        }
    }
}
