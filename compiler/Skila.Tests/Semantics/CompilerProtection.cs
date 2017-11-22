using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;

namespace Skila.Tests.Semantics
{
    // put here all tests that check if the compiler is robust enough
    // tests passes if it does not crash

    [TestClass]
    public class CompilerProtection
    {

        [TestMethod]
        public IErrorReporter Environment()
        {
            var env = Language.Environment.Create();

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularConversion()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            TypeDefinition type = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.Out))
                .Slicing(true)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                new[] {
                    // converting itself
                    FunctionParameter.Create("value", NameReference.Create("Foo", NameReference.Create("T")), Variadic.None,
                        null, isNameRequired: false) },
                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout("value")
                    }))
                ));

            var decl = root_ns.AddNode(
                VariableDefiniton.CreateStatement("x",
                    NameReference.Create("Foo", NameFactory.IntTypeReference()), IntLiteral.Create("5")));

            var resolver = NameResolver.Create(env);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CircularMutabilityCheck()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var chain_type = root_ns.AddBuilder(TypeBuilder.Create("Chain")
                // same type as current type -> circular reference
                .With(VariableDefiniton.CreateStatement("n", NameReference.Create("Chain"), Undef.Create())));

            var resolver = NameResolver.Create(env);

            return resolver;
        }
    }
}