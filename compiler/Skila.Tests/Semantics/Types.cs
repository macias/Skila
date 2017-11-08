﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        public IErrorReporter AutoDefaultConstructor()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point")).With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null)));

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
                    new[] { FunctionParameter.Create("a", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false) },
                    Block.CreateStatement())));
            VariableDeclaration field_decl = VariableDeclaration.CreateStatement("x", NameReference.Create("Bar"), null);
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
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .With(VariableDeclaration.CreateStatement("field", NameFactory.DoubleTypeReference(), null, EntityModifier.Static)));

            NameReference field_ref = NameReference.Create("f", "field");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                                ExpressionReadMode.OptionalUse,
                                NameFactory.DoubleTypeReference(),
                                Block.CreateStatement(new IExpression[] {
                                    VariableDeclaration.CreateStatement("f",NameReference.Create("Foo"),Undef.Create()),
                                    Return.Create(field_ref) })));

            var resolver = NameResolver.Create(env, new Options() { StaticMemberOnlyThroughTypeName = true });

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInstanceMemberReference()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference field_ref = NameReference.Create("field");
            root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .With(VariableDeclaration.CreateStatement("field", NameFactory.DoubleTypeReference(), null))
                .With(FunctionBuilder.Create(NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.DoubleTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(field_ref) }))
                    .Modifier(EntityModifier.Static)
                    .Build()));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InstanceMemberAccessInStaticContext, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNonAbstractTypeWithAbstractMethod()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_decl = FunctionDefinition.CreateDeclaration(EntityModifier.None,
                    NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference());
            FunctionDefinition abstract_func = FunctionDefinition.CreateFunction(EntityModifier.Abstract,
                    NameDefinition.Create("bar"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(), Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("3")) }));
            root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(func_decl)
                .With(abstract_func));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, func_decl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonAbstractTypeWithAbstractMethod, abstract_func));

            return resolver;
        }

    }

}
