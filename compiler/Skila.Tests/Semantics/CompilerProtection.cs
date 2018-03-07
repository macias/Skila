using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Expressions.Literals;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    // put here all tests to check if the compiler is robust enough
    // test passes if it does not crash (anything else HERE is irrelevant)

    [TestClass]
    public class CompilerProtection
    {
        [TestMethod]
        public IErrorReporter Environment()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true });

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter EnvironmentOption2()
        {
            var env = Language.Environment.Create(new Options()
            {
                DebugThrowOnError = true,
                StaticMemberOnlyThroughTypeName = true
            });

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter MiniEnvironment()
        {
            var env = Language.Environment.Create(new Options() { MiniEnvironment = true, DebugThrowOnError = true });

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter Internals()
        {
            var env = Language.Environment.Create(new Options() {});

            var resolver = NameResolver.Create(env);

            Assert.IsTrue(NameReference.CreateBaseInitReference().IsBaseInitReference);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularConversion()
        {
            var env = Language.Environment.Create(new Options() {});
            var root_ns = env.Root;

            TypeDefinition type = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.Out))
                .Slicing(true)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                new[] {
                    // converting itself
                    FunctionParameter.Create(NameFactory.SourceConvConstructorParameter, NameReference.Create("Foo", NameReference.Create("T")), Variadic.None,
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement())
                ));

            var decl = root_ns.AddNode(
                VariableDeclaration.CreateStatement("x",
                    NameReference.Create("Foo", NameFactory.Int64TypeReference()), Int64Literal.Create("5")));

            var resolver = NameResolver.Create(env);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CrossRecursiveCalls()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("foo")
                .With(FunctionBuilder.Create("a", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName, "b"))
                })))
                .With(FunctionBuilder.Create("b", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName, "a"))
                }))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularMutabilityCheck()
        {
            var env = Language.Environment.Create(new Options() {});
            var root_ns = env.Root;

            var chain_type = root_ns.AddBuilder(TypeBuilder.Create("Chain")
                // same type as current type -> circular reference
                .With(VariableDeclaration.CreateStatement("n", NameReference.Create("Chain"), Undef.Create())));

            var resolver = NameResolver.Create(env);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingFunctionAsProperty()
        {
            // when writing this test compiler crashed when the function was called like a property
            var env = Skila.Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            NameReference func_name = NameReference.Create("b", NameFactory.IterableCount);
            root_ns.AddBuilder(FunctionBuilder.Create("bad_call", NameFactory.SizeTypeReference(), Block.CreateStatement(
                // using function like a property (error)
                // todo: however it should be another error, because this reference should create functor and the error should
                // say about type mismatch between returning value and result type
                Return.Create(func_name)))
                .Parameters(FunctionParameter.Create("b", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference()),
                    Variadic.Create(2, 3), null, false))
                .Include(NameFactory.LinqExtensionReference()));

          
            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UndefinedTemplateArguments, func_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingTypeNameWithAlias()
        {
            var env = Language.Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                .SetModifier(EntityModifier.Base));

            TypeDefinition from_reg = root_ns.AddBuilder(TypeBuilder.CreateEnum(NameFactory.SizeTypeName)
                .Parents("Whatever")
                .SetModifier(EntityModifier.Base)
                .With(EnumCaseBuilder.Create("small", "big")));

            TypeDefinition from_enum = root_ns.AddBuilder(TypeBuilder.Create("Another")
                .Parents(NameFactory.SizeTypeName)
                .SetModifier(EntityModifier.Base));

            var resolver = NameResolver.Create(env);

            return resolver;
        }

    }
}