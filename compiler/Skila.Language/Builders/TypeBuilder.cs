using System;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using NaiveLanguageTools.Common;
using System.Collections.Generic;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Extensions;

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
        /*private TypeBuilder WithBaseEnum(string typename)
        {
            if (this.build != null)
                throw new InvalidOperationException();

            NameReference typename_ref = NameReference.Create(typename);
            this.embedTypeNames.Add(typename_ref);
            this.With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Implicit,
                 new[] { FunctionParameter.Create(NameFactory.SourceConvConstructorParameter, typename_ref,
                        ExpressionReadMode.CannotBeRead) },
                 Block.CreateStatement()));

            return this;
        }*/
        public static TypeBuilder CreateEnum(string name)
        {
            TypeBuilder builder = new TypeBuilder(NameDefinition.Create(name));
            builder = builder
                .SetModifier(EntityModifier.Enum)
                .Parents(NameFactory.IEquatableTypeReference())
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native | EntityModifier.Private,
                    new[] { FunctionParameter.Create(NameFactory.EnumConstructorParameter, NameFactory.NatTypeReference(),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))
                    // copy constructor
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Native,
                    new[] { FunctionParameter.Create(NameFactory.SourceCopyConstructorParameter, NameReference.Create(name),
                        ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement()))
                .With(FunctionBuilder.Create(NameFactory.ConvertFunctionName, ExpressionReadMode.ReadRequired, NameFactory.NatTypeReference(),
                    Block.CreateStatement())
                    .SetModifier(EntityModifier.Native))
                 // when enum inherits an enum it won't call super to check equality
                .WithEquatableEquals(EntityModifier.UnchainBase)
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .SetModifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", builder.CreateTypeNameReference(TypeMutability.ReadOnly), ExpressionReadMode.CannotBeRead)))
                .With(FunctionBuilder.Create(NameFactory.NotEqualOperator,
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .SetModifier(EntityModifier.Native)
                    .Parameters(FunctionParameter.Create("cmp", builder.CreateTypeNameReference(TypeMutability.ReadOnly), ExpressionReadMode.CannotBeRead)))
                    ;

            return builder;
        }
        public static TypeBuilder CreateInterface(string name, EntityModifier modifier = null)
        {
            return CreateInterface(NameDefinition.Create(name), modifier);
        }
        public static TypeBuilder CreateInterface(NameDefinition name, EntityModifier modifier = null)
        {
            return new TypeBuilder(name).SetModifier(EntityModifier.Interface | modifier);
        }


        private readonly NameDefinition name;
        private readonly List<INode> features;
        private IEnumerable<NameReference> parents;
        //private List<NameReference> embedTypeNames;
        public EntityModifier Modifier { get; private set; }
        private TypeDefinition build;
        private bool allowSlicing;
        private IEnumerable<TemplateConstraint> constraints;

        private TypeBuilder(NameDefinition name)
        {
            this.name = name;
            this.features = new List<INode>();
            //this.embedTypeNames = new List<NameReference>();
        }
        public TypeBuilder Parents(params NameReference[] parents)
        {
            if (this.build != null)
                throw new InvalidOperationException();

            this.parents = parents.Concat(this.parents??Enumerable.Empty<NameReference>()).StoreReadOnly();
            return this;
        }
        public NameReference CreateTypeNameReference(TypeMutability mutability = TypeMutability.None)
        {
            return this.name.CreateNameReference(prefix: null, mutability: mutability);
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
            if (!this.Modifier.HasEnum)
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

        public TypeBuilder SetModifier(EntityModifier modifier)
        {
            if (this.build != null)
                throw new InvalidOperationException();

            this.Modifier = modifier | this.Modifier;
            return this;
        }

        public TypeDefinition Build()
        {
            if (build == null)
            {
                // todo: maybe introduce IFeature to make distinction between active features and attributes?
                if (features.Any(it => it is EntityModifier))
                {
                    throw new Exception("Add modifier via Modifier method");
                }

                build = TypeDefinition.Create(this.Modifier,
                    this.name,
                    this.constraints,
                    allowSlicing,
                    this.parents,
                    // put fields first so when function refers to variable it is already evaluated (midly hackerish)
                    // this avoids clearing/restoring local names registry of the evaluated function
                    features.OrderBy(it => it is VariableDeclaration ? 0 : 1));
            }
            return build;
        }

        public static implicit operator TypeDefinition(TypeBuilder @this)
        {
            return @this.Build();
        }
    }
}
