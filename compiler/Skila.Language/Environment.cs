using System;
using System.Collections.Generic;
using System.Linq;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using NaiveLanguageTools.Common;
using Skila.Language.Flow;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class Environment
    {
        public static Environment Create()
        {
            return new Environment();
        }

        private readonly List<TypeDefinition> functionTypes;
        public IReadOnlyList<TypeDefinition> FunctionTypes => this.functionTypes;
        public TypeDefinition ReferenceType { get; }
        public TypeDefinition PointerType { get; }
        public TypeDefinition VoidType { get; }
        public TypeDefinition UnitType { get; }
        public TypeDefinition BoolType { get; }
        public TypeDefinition ExceptionType { get; }
        public TypeDefinition OptionType { get; }
        public TypeDefinition ChannelType { get; }

        public Namespace Root { get; }
        public Namespace SystemNamespace { get; }
        public Namespace ConcurrencyNamespace { get; }
        public Namespace CollectionsNamespace { get; }

        public TypeDefinition IntType { get; }
        // public TypeDefinition BoolType { get; }
        public TypeDefinition StringType { get; }
        public TypeDefinition DoubleType { get; }
        public TypeDefinition ObjectType { get; }
        public TypeDefinition ISequenceType { get; }
        public TypeDefinition IIterableType { get; }

        public FunctionDefinition OptionValueConstructor { get; }
        public FunctionDefinition OptionEmptyConstructor { get; }

        private Environment()
        {
            this.Root = Namespace.Create(NameFactory.RootNamespace);
            this.SystemNamespace = this.Root.AddNode(Namespace.Create(NameFactory.SystemNamespace));
            this.ConcurrencyNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.ConcurrencyNamespace));
            this.CollectionsNamespace = this.SystemNamespace.AddNode(Namespace.Create(NameFactory.CollectionsNamespace));

            this.ObjectType = this.Root.AddNode(TypeDefinition.CreateProtocol(EntityModifier.Const,
                NameDefinition.Create(NameFactory.ObjectTypeName)));

            this.IntType = this.Root.AddNode(TypeBuilder.Create(NameFactory.IntTypeName)
                .Plain(true)
                .Modifier(EntityModifier.Const)
                .Parents(NameFactory.ObjectTypeReference())
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Public, null, Block.CreateStatement()))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                    new[] { FunctionParameter.Create("source", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false) }, 
                    Block.CreateStatement()))
                .With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.AddOperator),
                    new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))
                .Build());

            this.DoubleType = this.Root.AddNode(TypeBuilder.Create(NameFactory.DoubleTypeName)
                .Plain(true)
                .Modifier(EntityModifier.Const)
                .Parents(NameFactory.ObjectTypeReference()).Build());

            // spread functions family
            {
                // no limits
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] { FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"), Variadic.None, null, isNameRequired: false) },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] { decl, Return.Create(NameReference.Create("result")) })));
            }
            {
                // with min limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                   new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"), Variadic.None, null, isNameRequired: false),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false),
                   },
                   ExpressionReadMode.ReadRequired,
                   NameFactory.ISequenceTypeReference("T"),
                   Block.CreateStatement(new IExpression[] { decl, Return.Create(NameReference.Create("result")) })));
            }
            {
                // with min+max limit
                var decl = VariableDeclaration.CreateStatement("result", NameFactory.ISequenceTypeReference("T"), Undef.Create());
                this.SystemNamespace.AddBuilder(FunctionBuilder.Create(NameDefinition.Create(NameFactory.SpreadFunctionName, "T", VarianceMode.None),
                    new[] {
                        FunctionParameter.Create("coll", NameFactory.IIterableTypeReference("T"), Variadic.None, null, isNameRequired: false),
                        FunctionParameter.Create("min", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false),
                        FunctionParameter.Create("max", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false),
                    },
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ISequenceTypeReference("T"),
                    Block.CreateStatement(new IExpression[] { decl, Return.Create(NameReference.Create("result")) })));

            }

            this.ISequenceType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.ISequenceTypeName, "T", VarianceMode.Out))
                    .Modifier(EntityModifier.Protocol)
                    .Parents(NameFactory.ObjectTypeReference()));
            this.IIterableType = this.CollectionsNamespace.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create(NameFactory.IIterableTypeName, "T", VarianceMode.Out))
                    .Modifier(EntityModifier.Protocol)
                    .Parents(NameFactory.ObjectTypeReference()));

            this.functionTypes = new List<TypeDefinition>();

            //            this.ObjectType = root.AddNode(TypeDefinition.CreateValue(EntityModifier.None, NameDefinition.Create(NameFactory.ObjectTypeName)));
            this.VoidType = Root.AddBuilder(TypeBuilder.Create(NameFactory.VoidTypeName)
                .Modifier(EntityModifier.Const)
                .Plain(true));

            this.BoolType = Root.AddBuilder(TypeBuilder.Create(NameFactory.BoolTypeName)
                .Plain(true)
                .Modifier(EntityModifier.Const)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Public, null, Block.CreateStatement()))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                    new[] { FunctionParameter.Create("source", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false) }, 
                    Block.CreateStatement()))
                .With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.NotOperator),
                    null,
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))
                .Parents(NameFactory.ObjectTypeReference()));

            this.UnitType = Root.AddBuilder(TypeBuilder.Create(NameFactory.UnitTypeName)
                .Modifier(EntityModifier.Const)
                .Plain(true)
                .Parents(NameFactory.ObjectTypeReference()));
            // pointer and reference are not of Object type (otherwise we could have common root for String and pointer to Int)
            this.ReferenceType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.ReferenceTypeName, "T", VarianceMode.Out))
                .Plain(true)
                .Modifier(EntityModifier.Const)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Private, null, Block.CreateStatement()))
                .Slicing(true));
            /*  this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));
              this.ReferenceType.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                  new[] { FunctionParameter.Create("value", NameFactory.PointerTypeReference(NameReference.Create("T")), Variadic.None, null, isNameRequired: false) },
                  Block.CreateStatement(new IExpression[] { })));*/
            this.PointerType = Root.AddBuilder(TypeBuilder.Create(NameDefinition.Create(NameFactory.PointerTypeName, "T", VarianceMode.Out))
                .Plain(true)
                .Modifier(EntityModifier.Const)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Private, null, Block.CreateStatement()))
                .Slicing(true));

            /*this.PointerType.AddNode(FunctionDefinition.CreateFunction(EntityModifier.Implicit, NameDefinition.Create(NameFactory.ConvertFunctionName),
                null, ExpressionReadMode.ReadRequired, NameReference.Create("T"),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));*/

            this.StringType = this.SystemNamespace.AddNode(TypeBuilder.Create(NameFactory.StringTypeName)
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Const)
                //.Slicing(true)
                .Parents(NameFactory.ObjectTypeReference()).Build());

            this.ChannelType = this.ConcurrencyNamespace.AddNode(createChannelType());

            this.ExceptionType = this.SystemNamespace.AddNode(TypeBuilder.Create(NameFactory.ExceptionTypeName)
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Const)
                .Parents(NameFactory.ObjectTypeReference()).Build());

            {
                FunctionDefinition empty, value;
                this.OptionType = this.SystemNamespace.AddNode(createOptionType(out empty,out value));
                this.OptionEmptyConstructor = empty;
                this.OptionValueConstructor = value;
            }

            foreach (int count in Enumerable.Range(0, 15))
                this.functionTypes.Add(createFunction(Root, count));
            this.functionTypes.ForEach(it => Root.AddNode(it));
        }

        private static TypeDefinition createChannelType()
        {
            return TypeBuilder.Create(NameDefinition.Create(NameFactory.ChannelTypeName,
                    TemplateParametersBuffer.Create().Add("T", VarianceMode.None, EntityModifier.Const, null, null).Values))
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Const)
                .With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelSend),
                    new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(), Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    })))
                .With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelClose),
                    null,
                    ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), Block.CreateStatement()))
                .With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelReceive),
                    null,
                    ExpressionReadMode.ReadRequired, NameFactory.OptionTypeReference(NameReference.Create("T")),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))
                /*.With(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.ChannelTryReceive),
                    null,
                    ExpressionReadMode.ReadRequired, NameFactory.OptionTypeReference(NameReference.Create("T")),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })))*/
                .Parents(NameFactory.ObjectTypeReference())
                .Plain(true)
                .Build();
        }

        private TypeDefinition createOptionType(out FunctionDefinition empty_constructor, out FunctionDefinition value_constructor)
        {
            const string value_field = "value";
            const string has_value_field = "hasValue";

            value_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                new[] { FunctionParameter.Create("value", NameReference.Create("T"), Variadic.None, null, isNameRequired: false) },
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"value"),
                                            NameReference.Create("value")),
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,
                                            has_value_field), BoolLiteral.CreateTrue())
                                    }));

            empty_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.Public,
                                null,
                                    Block.CreateStatement(new[] {
                                        Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,has_value_field),
                                            BoolLiteral.CreateFalse())
                                    }));

            return TypeBuilder.Create(
NameDefinition.Create(NameFactory.OptionTypeName,
TemplateParametersBuffer.Create().Add("T", VarianceMode.Out).Values))
                            .With(Property.Create(NameFactory.OptionHasValue, NameFactory.BoolTypeReference(),
                                null,
                                new[] { Property.CreateProxyGetter(NameFactory.BoolTypeReference(), NameReference.Create(has_value_field)) },
                                null
                            ))
                            .With(Property.Create(NameFactory.OptionValue, NameReference.Create("T"),
                                null,
                                new[] { FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create(NameFactory.PropertyGetter),
                                null, ExpressionReadMode.CannotBeRead, NameReference.Create("T"),
                                Block.CreateStatement(new IExpression[] {
                                    IfBranch.CreateIf(ExpressionFactory.NotOperator( NameReference.Create(has_value_field)),
                                        new[]{ Throw.Create(ExpressionFactory.HeapConstructorCall(NameFactory.ExceptionTypeReference())) }),
                                    Return.Create(NameReference.Create(value_field))
                                })) },
                                null
                            ))
                            .With(VariableDeclaration.CreateStatement(value_field, NameReference.Create("T"), Undef.Create()))
                            .With(VariableDeclaration.CreateStatement(has_value_field, NameFactory.BoolTypeReference(), Undef.Create()))
                            .With(empty_constructor)
                            .With(value_constructor)
                            .Parents(NameFactory.ObjectTypeReference())
                            .Build();
        }

        private TypeDefinition createFunction(Namespace root, int paramCount)
        {
            var type_parameters = TemplateParametersBuffer.Create();
            var function_parameters = new List<FunctionParameter>();
            foreach (int i in Enumerable.Range(0, paramCount))
            {
                var type_name = $"T{i}";
                type_parameters.Add(type_name, VarianceMode.In);
                function_parameters.Add(FunctionParameter.Create($"item{i}", NameReference.Create(type_name), Variadic.None, null, isNameRequired: false));
            }
            type_parameters.Add("R", VarianceMode.Out);

            var function_def = TypeDefinition.CreateFunctor(NameDefinition.Create(NameFactory.FunctionTypeName, type_parameters.Values),
                new FunctorSignature(function_parameters, NameReference.Create("R")));

            return function_def;
        }

        public bool IsFunctionType(EntityInstance instance)
        {
            if (instance == null)
                return false;

            return (instance != null && functionTypes.Any(it => it == instance.Target));
        }

        internal bool IsOfVoidType(INameReference typeName)
        {
            return typeName != null && IsVoidType(typeName.Evaluation);//.IsSame(this.VoidType.InstanceOf, jokerMatchesAll: false);
        }

        public bool IsVoidType(IEntityInstance typeInstance)
        {
            //            return VoidType.InstanceOf.MatchesTarget(typeInstance, allowSlicing: false);
            return VoidType.InstanceOf.IsSame(typeInstance, jokerMatchesAll: false);
        }
        public bool IsReferenceOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType(ReferenceType));
        }
        public bool IsPointerOfType(IEntityInstance instance)
        {
            return instance.Enumerate().All(it => it.IsOfType( PointerType));
        }

        public bool IsOfUnitType(INameReference typeName)
        {
            return typeName.Evaluation.IsSame(this.UnitType.InstanceOf, jokerMatchesAll: false);
        }
    }
}
