﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class NameResolution
    {
        [TestMethod]
        public IErrorReporter ErrorDuplicateType()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None)));

            TypeDefinition second_type = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None))
                .Constraints(ConstraintBuilder.Create("T")
                    .Modifier(EntityModifier.Const)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NameAlreadyExists, second_type));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ResolvingIt()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Base)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null, EntityModifier.Private | EntityModifier.Static))
                .With(FunctionBuilder.Create("getIt", ExpressionReadMode.OptionalUse, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(NameReference.Create(NameFactory.ItTypeName,"x"))
                    }))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMissingThis()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Base)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null, EntityModifier.Protected))
                .With(FunctionBuilder.Create("foo", ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement())));

            NameReference x_ref = NameReference.Create("x");
            NameReference y_ref = NameReference.Create("y");
            NameReference foo_ref = NameReference.Create("foo");
            NameReference bar_ref = NameReference.Create("bar");
            root_ns.AddBuilder(TypeBuilder.Create("Next")
                .Parents("Point")
                .With(VariableDeclaration.CreateStatement("y", NameFactory.IntTypeReference(), null))
                .With(FunctionBuilder.Create("bar", ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement()))
                .With(FunctionBuilder.Create("all", ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new IExpression[] {
                        ExpressionFactory.Readout(x_ref),
                        ExpressionFactory.Readout(y_ref),
                        FunctionCall.Create(foo_ref),
                        FunctionCall.Create(bar_ref),
                    }))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingThisPrefix, x_ref));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingThisPrefix, y_ref));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingThisPrefix, foo_ref));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingThisPrefix, bar_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAccessForbidden()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(FunctionBuilder.Create("dummyReader", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout(NameFactory.ThisVariableName,"x")
                    })))
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null,
                    EntityModifier.Private | EntityModifier.Reassignable)));

            NameReference private_ref = NameReference.Create("p", "x");
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor("Point")),
                    Assignment.CreateStatement(private_ref,IntLiteral.Create("5")),
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AccessForbidden, private_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCrossReferencingBaseMember()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                .With(VariableDeclaration.CreateStatement("a", NameFactory.IntTypeReference(), null,
                    EntityModifier.Protected))
                .Modifier(EntityModifier.Base));

            NameReference cross_reference = NameReference.Create(NameFactory.BaseVariableName, "a");
            root_ns.AddBuilder(TypeBuilder.Create("Bank")
                .Parents("Keeper")
                .With(FunctionBuilder.Create(NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.Readout(cross_reference),
                })))
                .Modifier(EntityModifier.Base));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CrossReferencingBaseMember, cross_reference));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ScopeShadowing()
        {
            var env = Environment.Create(new Options() { ScopeShadowing = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,IntLiteral.Create("2")),
                    Block.CreateStatement(new IExpression[]{
                        // shadowing
                        VariableDeclaration.CreateStatement("x", null, BoolLiteral.CreateFalse()),
                        VariableDeclaration.CreateStatement("a",NameFactory.BoolTypeReference(),NameReference.Create("x")),
                        ExpressionFactory.Readout("a"),
                    }),
                    VariableDeclaration.CreateStatement("b",NameFactory.IntTypeReference(),NameReference.Create("x")),
                    ExpressionFactory.Readout("b"),
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorScopeShadowing()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            VariableDeclaration decl = VariableDeclaration.CreateStatement("x", null, BoolLiteral.CreateFalse());
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,IntLiteral.Create("2")),
                    Block.CreateStatement(new IExpression[]{
                        // shadowing
                        decl,
                    }),
                    ExpressionFactory.Readout("x"),
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NameAlreadyExists, decl.Name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReservedKeyword()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            VariableDeclaration decl = VariableDeclaration.CreateExpression(NameFactory.SelfFunctionName, null, IntLiteral.Create("3"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.Readout( decl)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReservedName, decl.Name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingBeforeDefinition()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var x_ref = NameReference.Create("x");
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foox"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("a", NameFactory.IntTypeReference(), x_ref),
                    VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("1")),
                    ExpressionFactory.Readout("a"),
                    ExpressionFactory.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(x_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorCircularReference()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var x_ref = NameReference.Create("x");
            var decl = VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), x_ref);

            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CircularReference, decl));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorDuplicatedName()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("1"),
                modifier: EntityModifier.Public));
            var second_decl = root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(),
                IntLiteral.Create("2"), modifier: EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NameAlreadyExists, second_decl));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingQualifiedReferenceToNestedTarget()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            // reference to nested target
            var string_ref = root_ns.AddNode(NameReference.Create(NameFactory.SystemNamespaceReference(), NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingQualifiedReferenceInSameNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            // reference to the target in the same namespace
            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.SystemNamespaceReference(), NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorResolvingUnqualifiedReferenceToNestedNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            // incorrect, wrong namespace
            var string_ref = root_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, string_ref.Binding.Matches.Count());
            Assert.IsFalse(string_ref.Binding.HasMatch);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingUnqualifiedReferenceWithinSameNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingForDuplicatedType()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var dup_type = TypeBuilder.Create(NameDefinition.Create(NameFactory.StringTypeName)).Build();
            system_ns.AddNode(dup_type);

            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, string_ref.Binding.Matches.Count());
            Assert.IsTrue(string_ref.Binding.Matches.Any(it => it.Target == env.StringType));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateResolving()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var tuple_ref = NameReference.Create("Tuple", NameReference.Create("T"));

            var tuple_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Tuple", "T", VarianceMode.None))
                .With(tuple_ref));
            var abc_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("ABC")));
            var derived_type = system_ns.AddBuilder(TypeBuilder.Create("Deriv").Parents(NameReference.Create("ABC")));

            var tuple_abc_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("ABC")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, tuple_ref.Binding.Matches.Count());
            Assert.AreEqual(tuple_type, tuple_ref.Binding.Match.Target);
            Assert.AreEqual(tuple_type.NestedTypes().Single(),
                tuple_ref.Binding.Match.TemplateArguments.Single().Target());

            Assert.AreEqual(1, tuple_abc_ref.Binding.Matches.Count());
            Assert.AreEqual(tuple_type, tuple_abc_ref.Binding.Match.Target);
            Assert.AreEqual(abc_type, tuple_abc_ref.Binding.Match.TemplateArguments.Single().Target());

            return resolver;
        }

    }
}
