using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Types
    {

        [TestMethod]
        public IErrorReporter ErrorInOutVariance()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            NameReference fielda_typename = NameReference.Create("TA");
            NameReference fieldb_typename = NameReference.Create("TB");
            NameReference propa_typename = NameReference.Create("TA");
            NameReference propb_typename = NameReference.Create("TB");
            root_ns.AddBuilder(TypeBuilder.Create(
                NameDefinition.Create(NameFactory.TupleTypeName,
                TemplateParametersBuffer.Create().Add("TA", VarianceMode.In).Add("TB", VarianceMode.Out).Values))
                .Modifier(EntityModifier.Mutable)
                .With(ExpressionFactory.BasicConstructor(new[] { "adata", "bdata" },
                    new[] { NameReference.Create("TA"), NameReference.Create("TB") }))
                .With(VariableDeclaration.CreateStatement("fa", fielda_typename, Undef.Create(), EntityModifier.Reassignable | EntityModifier.Public))
                .With(VariableDeclaration.CreateStatement("fb", fieldb_typename, Undef.Create(), EntityModifier.Reassignable | EntityModifier.Public))
                .With(PropertyBuilder.CreateAutoFull("adata", propa_typename, Undef.Create()))
                .With(PropertyBuilder.CreateAutoFull("bdata", propb_typename, Undef.Create())));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, fielda_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, fieldb_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, propa_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VarianceForbiddenPosition, propb_typename));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CatVarianceExample() // Programming in Scala, 2nd ed, p. 399
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            NameReference result_typename = NameReference.Create("Cat",
                        NameReference.Create("Cat", NameReference.Create("U"), NameReference.Create("T")), NameReference.Create("U"));

            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("Cat", TemplateParametersBuffer.Create()
                .Add("T", VarianceMode.In).Add("U", VarianceMode.Out).Values))
                .With(FunctionBuilder.CreateDeclaration(NameDefinition.Create("meow", TemplateParametersBuffer.Create()
                    .Add("W", VarianceMode.In).Values), result_typename)
                    .Parameters(FunctionParameter.Create("volume", NameReference.Create("T")),
                        FunctionParameter.Create("listener",
                            NameReference.Create("Cat", NameReference.Create("U"), NameReference.Create("T"))))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularPointerNesting()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCircularValueNesting()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NestedValueOfItself, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NestedValueOfItself, decl2));

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorConflictingModifier()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                .Modifier(EntityModifier.Const | EntityModifier.Mutable));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConflictingModifier, type_def.Modifier));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AutoDefaultConstructor()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null, EntityModifier.Public)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(type_def.HasDefaultPublicConstructor());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNoDefaultConstructor()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var bar_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Bar"))
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                    new[] { FunctionParameter.Create("a", NameFactory.IntTypeReference(),
                        Variadic.None, null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement())));
            VariableDeclaration field_decl = VariableDeclaration.CreateStatement("x", NameReference.Create("Bar"), null,
                EntityModifier.Public);
            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                .With(field_decl));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NoDefaultConstructor, field_decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorStaticMemberReference()
        {
            var env = Environment.Create(new Options() { StaticMemberOnlyThroughTypeName = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .With(VariableDeclaration.CreateStatement("field", NameFactory.DoubleTypeReference(), null,
                    EntityModifier.Static | EntityModifier.Public)));

            NameReference field_ref = NameReference.Create("f", "field");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                                ExpressionReadMode.OptionalUse,
                                NameFactory.DoubleTypeReference(),
                                Block.CreateStatement(new IExpression[] {
                                    VariableDeclaration.CreateStatement("f",NameReference.Create("Foo"),Undef.Create()),
                                    Return.Create(field_ref) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.StaticMemberAccessInInstanceContext, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInstanceMemberReference()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference field_ref1 = NameReference.Create("field");

            root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .With(VariableDeclaration.CreateStatement("field", NameFactory.DoubleTypeReference(), null, EntityModifier.Public))
                .With(FunctionBuilder.Create(NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.DoubleTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(field_ref1) }))
                    .Modifier(EntityModifier.Static)));

            NameReference field_ref2 = NameReference.Create("Foo", "field");

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("some_func"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.DoubleTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                                    Return.Create(field_ref2) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InstanceMemberAccessInStaticContext, field_ref1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InstanceMemberAccessInStaticContext, field_ref2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIncorrectMethodsForType()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_decl = FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("foo"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference());
            FunctionDefinition abstract_func = FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(), Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("3")) }))
                    .Modifier(EntityModifier.Abstract);
            FunctionDefinition base_func = FunctionBuilder.Create(
                    NameDefinition.Create("basic"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(), Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("3")) }))
                    .Modifier(EntityModifier.Base);
            root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(func_decl)
                .With(base_func)
                .With(abstract_func));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, func_decl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, abstract_func));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SealedTypeWithBaseMethod, base_func));

            return resolver;
        }

    }

}
