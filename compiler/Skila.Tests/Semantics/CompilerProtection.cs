using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Expressions.Literals;

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
        public IErrorReporter ConflictingTypeNameWithAlias()
        {
            var env = Language.Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                .Modifier(EntityModifier.Base));

            TypeDefinition from_reg = root_ns.AddBuilder(TypeBuilder.CreateEnum(NameFactory.SizeTypeName)
                .Parents("Whatever")
                .Modifier(EntityModifier.Base)
                .With(EnumCaseBuilder.Create("small", "big")));

            TypeDefinition from_enum = root_ns.AddBuilder(TypeBuilder.Create("Another")
                .Parents(NameFactory.SizeTypeName)
                .Modifier(EntityModifier.Base));

            var resolver = NameResolver.Create(env);

            return resolver;
        }

    }
}