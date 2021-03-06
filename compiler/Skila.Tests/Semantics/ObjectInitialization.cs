﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Expressions.Literals;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class ObjectInitialization : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorCustomGetterWithInitialization()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                // error: custom getter (with no setter) + post initialization
                Property property = PropertyBuilder.Create(env.Options, "x", ()=>NameFactory.Nat8NameReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(Return.Create(Nat8Literal.Create("3")))))
                        .SetModifier(EntityModifier.PostInitialization);

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(property));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,
                        ConstructorCall.StackConstructor(NameReference.Create("Point"))
                        // this cannot be executed but do not report an error for it, because the type has to be fixed first
                        .Init("x",Nat8Literal.Create("17"))
                        .Build()),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PostInitializedCustomGetter, property));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInitializingWithGetter()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(PropertyBuilder.CreateAutoGetter(env.Options, "x", NameFactory.Nat8NameReference()))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("5"))
                        ))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,
                        ConstructorCall.StackConstructor(NameReference.Create("Point"))
                        // cannot use getter in post-initialization
                        .Init("x",Nat8Literal.Create("17"),out Assignment post_init)
                        .Build()),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, post_init));
            }

            return resolver;
        }
    }
}
