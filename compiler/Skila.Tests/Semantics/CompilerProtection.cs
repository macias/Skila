﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Expressions;

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

            var type = root_ns.AddNode(TypeDefinition.Create(EntityModifier.None,
                NameDefinition.Create("Foo", "T", VarianceMode.Out), allowSlicing: true, parents:null,
                entities:new[] { FunctionDefinition.CreateInitConstructor(EntityModifier.Implicit,
                new[] {
                    // converting itself
                    FunctionParameter.Create("value", NameReference.Create("Foo", NameReference.Create("T")), Variadic.None,
                    null, isNameRequired: false) },
                    Block.CreateStatement(new IExpression[] { }))
                }));

            var decl = root_ns.AddNode(
                VariableDeclaration.CreateStatement("x",
                    NameReference.Create("Foo", NameFactory.IntTypeReference()), IntLiteral.Create("5")));

            var resolver = NameResolver.Create(env);

            return resolver;
        }
    }
}