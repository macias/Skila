using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class Types
    {
        [TestMethod]
        public IErrorReporter ErrorSelfTypeUsage()
        {
            NameResolver resolver = null;
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                NameReference invalid_self1 = NameFactory.SelfTypeReference();
                NameReference invalid_self2 = NameFactory.SelfTypeReference();

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
        public IErrorReporter ErrorInOutVariance()
        {
            NameResolver resolver = null;
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                .SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                NameReference fielda_typename = NameReference.Create("TA");
                NameReference fieldb_typename = NameReference.Create("TB");
                NameReference propa_typename = NameReference.Create("TA");
                NameReference propb_typename = NameReference.Create("TB");
                root_ns.AddBuilder(TypeBuilder.Create(
                    NameDefinition.Create(NameFactory.TupleTypeName,
                    TemplateParametersBuffer.Create().Add("TA", VarianceMode.In).Add("TB", VarianceMode.Out).Values))
                    .SetModifier(EntityModifier.Mutable)
                    .With(ExpressionFactory.BasicConstructor(new[] { "adata", "bdata" },
                        new[] { NameReference.Create("TA"), NameReference.Create("TB") }))
                    .With(VariableDeclaration.CreateStatement("fa", fielda_typename, Undef.Create(),
                        env.Options.ReassignableModifier() | EntityModifier.Public))
                    .With(VariableDeclaration.CreateStatement("fb", fieldb_typename, Undef.Create(),
                        env.Options.ReassignableModifier() | EntityModifier.Public))
                    .With(PropertyBuilder.CreateAutoFull(env.Options, "adata", propa_typename, Undef.Create()))
                    .With(PropertyBuilder.CreateAutoFull(env.Options, "bdata", propb_typename, Undef.Create())));

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, fielda_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, fieldb_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, propa_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, propb_typename));
                Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CatVarianceExample() // Programming in Scala, 2nd ed, p. 399
        {
            NameResolver resolver = null;
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Shape"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(ExpressionFactory.Readout(NameReference.CreateThised("s")))))
                    .With(VariableDeclaration.CreateStatement("s", NameFactory.PointerTypeReference(NameReference.Create("Form")),
                    Undef.Create(), EntityModifier.Private)));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Form"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(ExpressionFactory.Readout(NameReference.CreateThised("f")))))
                    .With(VariableDeclaration.CreateStatement("f", NameFactory.PointerTypeReference(NameReference.Create("Shape")),
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                VariableDeclaration decl1 = VariableDeclaration.CreateStatement("s", NameReference.Create("Form"), null, EntityModifier.Private);
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Shape"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(ExpressionFactory.Readout(NameReference.CreateThised("s")))))
                    .With(decl1));

                VariableDeclaration decl2 = VariableDeclaration.CreateStatement("f", NameReference.Create("Shape"), null, EntityModifier.Private);
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Form"))
                    .With(FunctionBuilder.Create("reader", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(ExpressionFactory.Readout(NameReference.CreateThised("f")))))
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                    .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), null, EntityModifier.Public)));

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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                var bar_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Bar"))
                    .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                        new[] { FunctionParameter.Create("a", NameFactory.Int64TypeReference(),
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { StaticMemberOnlyThroughTypeName = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Foo")
                    .With(VariableDeclaration.CreateStatement("field", NameFactory.RealTypeReference(), null,
                        EntityModifier.Static | EntityModifier.Public)));

                NameReference field_ref = NameReference.Create("f", "field");
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                                    ExpressionReadMode.OptionalUse,
                                    NameFactory.RealTypeReference(),
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                NameReference field_ref1 = NameReference.Create("field");

                root_ns.AddBuilder(TypeBuilder.Create("Foo")
                    .With(VariableDeclaration.CreateStatement("field", NameFactory.RealTypeReference(), null, EntityModifier.Public))
                    .With(FunctionBuilder.Create("foo",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.RealTypeReference(),
                        Block.CreateStatement(new[] { Return.Create(field_ref1) }))
                        .SetModifier(EntityModifier.Static)));

                NameReference field_ref2 = NameReference.Create("Foo", "field");

                root_ns.AddBuilder(FunctionBuilder.Create("some_func",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.RealTypeReference(),
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
            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                FunctionDefinition func_decl = FunctionBuilder.CreateDeclaration(
                        "foo",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64TypeReference());
                FunctionDefinition abstract_func = FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64TypeReference(), Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("3")) }))
                        .SetModifier(EntityModifier.Abstract);
                FunctionDefinition base_func = FunctionBuilder.Create(
                        "basic",
                        ExpressionReadMode.OptionalUse,
                        NameFactory.Int64TypeReference(), Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("3")) }))
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
