﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using Skila.Language.Extensions;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Types : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorSelfTypeUsage()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference invalid_self1 = NameFactory.SelfNameReference();
                NameReference invalid_self2 = NameFactory.SelfNameReference();

                // in time probably we will use Self type in more places, but for now we forbid everything we don't support
                root_ns.AddBuilder(TypeBuilder.Create("What")
                    .With(FunctionBuilder.Create("foo", invalid_self1,
                        Block.CreateStatement(Return.Create(NameReference.Create("x"))))
                        .Parameters(FunctionParameter.Create("x", invalid_self2))));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelfTypeOutsideConstructor, invalid_self1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelfTypeOutsideConstructor, invalid_self2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInOutVarianceFields()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                .SetMutability(mutability));
                var root_ns = env.Root;

                NameReference field_a_typename = NameReference.Create("TA");
                NameReference field_b_typename = NameReference.Create("TB");

                root_ns.AddBuilder(TypeBuilder.Create(
                    NameDefinition.Create(NameFactory.TupleTypeName,
                    TemplateParametersBuffer.Create().Add("TA", VarianceMode.In).Add("TB", VarianceMode.Out).Values))
                    .SetModifier(EntityModifier.Mutable)

                    .With(VariableDeclaration.CreateStatement("fa", field_a_typename, Undef.Create(),
                        env.Options.ReassignableModifier() | EntityModifier.Public))

                    .With(VariableDeclaration.CreateStatement("fb", field_b_typename, Undef.Create(),
                        env.Options.ReassignableModifier() | EntityModifier.Public)));


                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, field_a_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, field_b_typename));
                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInOutVarianceProperties()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                .SetMutability(mutability));
                var root_ns = env.Root;

                NameReference prop_a_typename = NameReference.Create("TA");
                NameReference prop_b_typename = NameReference.Create("TB");

                root_ns.AddBuilder(TypeBuilder.Create(
                    NameDefinition.Create(NameFactory.TupleTypeName,
                    TemplateParametersBuffer.Create().Add("TA", VarianceMode.In).Add("TB", VarianceMode.Out).Values))
                    .SetModifier(EntityModifier.Mutable)

                    .With(ExpressionFactory.BasicConstructor(new[] { "adata", "bdata" },
                        new[] { NameReference.Create("TA"), NameReference.Create("TB") }))

                    .With(PropertyBuilder.CreateAutoFull(env.Options, "adata", prop_a_typename, Undef.Create()))

                    .With(PropertyBuilder.CreateAutoFull(env.Options, "bdata", prop_b_typename, Undef.Create())));

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, prop_a_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, prop_b_typename));
                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CatVarianceExample() // Programming in Scala, 2nd ed, p. 399
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference result_typename = NameReference.Create("Cat",
                            NameReference.Create("Cat", NameReference.Create("U"), NameReference.Create("T")), NameReference.Create("U"));

                root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("Cat", TemplateParametersBuffer.Create()
                    .Add("T", VarianceMode.In).Add("U", VarianceMode.Out).Values))
                    .With(FunctionBuilder.CreateDeclaration("meow", "W", VarianceMode.In, ExpressionReadMode.ReadRequired, result_typename)
                        .Parameters(FunctionParameter.Create("volume", NameReference.Create("T")),
                            FunctionParameter.Create("listener",
                                NameReference.Create("Cat", NameReference.Create("U"), NameReference.Create("T"))))));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularPointerNesting()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Shape"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitNameReference(),
                        Block.CreateStatement( ExpressionFactory.Readout(NameReference.CreateThised("s")))))
                    .With(VariableDeclaration.CreateStatement("s", NameFactory.PointerNameReference(NameReference.Create("Form")),
                    Undef.Create(), EntityModifier.Private)));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Form"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitNameReference(),
                        Block.CreateStatement( ExpressionFactory.Readout(NameReference.CreateThised("f")))))
                    .With(VariableDeclaration.CreateStatement("f", NameFactory.PointerNameReference(NameReference.Create("Shape")),
                    Undef.Create(), EntityModifier.Private)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCircularValueNesting()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl1 = VariableDeclaration.CreateStatement("s", NameReference.Create("Form"), null, EntityModifier.Private);
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Shape"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitNameReference(),
                        Block.CreateStatement( ExpressionFactory.Readout(NameReference.CreateThised("s")))))
                    .With(decl1));

                VariableDeclaration decl2 = VariableDeclaration.CreateStatement("f", NameReference.Create("Shape"), null, EntityModifier.Private);
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Form"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitNameReference(),
                        Block.CreateStatement( ExpressionFactory.Readout(NameReference.CreateThised("f")))))
                    .With(decl2));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NestedValueOfItself, decl1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NestedValueOfItself, decl2));
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorConflictingModifier()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                    .SetModifier(EntityModifier.Const | EntityModifier.Mutable));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConflictingModifier, type_def.Modifier));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AutoDefaultConstructor()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                    .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), null, EntityModifier.Public)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(type_def.HasDefaultPublicConstructor());
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNoDefaultConstructor()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var bar_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Bar"))
                    .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                        new[] { FunctionParameter.Create("a", NameFactory.Int64NameReference(),
                        Variadic.None, null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead) },
                        Block.CreateStatement())));
                VariableDeclaration field_decl = VariableDeclaration.CreateStatement("x", NameReference.Create("Bar"), null,
                    EntityModifier.Public);
                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                    .With(field_decl));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NoDefaultConstructor, field_decl));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorStaticMemberReference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { StaticMemberOnlyThroughTypeName = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Foo")
                    .With(VariableDeclaration.CreateStatement("field", NameFactory.RealNameReference(), null,
                        EntityModifier.Static | EntityModifier.Public)));

                NameReference field_ref = NameReference.Create("f", "field");
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                                    ExpressionReadMode.OptionalUse,
                                    NameFactory.RealNameReference(),
                                    Block.CreateStatement(new IExpression[] {
                                    VariableDeclaration.CreateStatement("f",NameReference.Create("Foo"),Undef.Create()),
                                    Return.Create(field_ref) })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.StaticMemberAccessInInstanceContext, field_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInstanceMemberReference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference field_ref1 = NameReference.Create("field");

                root_ns.AddBuilder(TypeBuilder.Create("Foo")
                    .With(VariableDeclaration.CreateStatement("field", NameFactory.RealNameReference(), null, EntityModifier.Public))
                    .With(FunctionBuilder.Create("foo",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.RealNameReference(),
                        Block.CreateStatement(new[] { Return.Create(field_ref1) }))
                        .SetModifier(EntityModifier.Static)));

                NameReference field_ref2 = NameReference.Create("Foo", "field");

                root_ns.AddBuilder(FunctionBuilder.Create("some_func",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.RealNameReference(),
                        Block.CreateStatement(new IExpression[] {
                                    Return.Create(field_ref2) })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InstanceMemberAccessInStaticContext, field_ref1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InstanceMemberAccessInStaticContext, field_ref2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIncorrectMethodsForType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_decl = FunctionBuilder.CreateDeclaration(
                        "foo",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64NameReference());
                FunctionDefinition abstract_func = FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64NameReference(), Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("3")) }))
                        .SetModifier(EntityModifier.Abstract);
                FunctionDefinition base_func = FunctionBuilder.Create(
                        "basic",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64NameReference(), Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("3")) }))
                        .SetModifier(EntityModifier.Base);
                root_ns.AddBuilder(TypeBuilder.Create("X")
                    .With(func_decl)
                    .With(base_func)
                    .With(abstract_func));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, func_decl));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, abstract_func));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SealedTypeWithBaseMethod, base_func));
            }

            return resolver;
        }

    }

}
