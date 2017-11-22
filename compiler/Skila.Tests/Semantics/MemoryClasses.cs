﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class MemoryClasses
    {
        [TestMethod]
        public IErrorReporter ErrorPersistentReferenceType()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var decl1 = VariableDefiniton.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create());
            root_ns.AddNode(decl1);

            var decl2 = VariableDefiniton.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Static);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl2,
                    Tools.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorHeapTypeOnStack()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var decl = VariableDefiniton.CreateStatement("bar", NameFactory.StringTypeReference(),
                initValue: StringLiteral.Create("hi"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl,
                    Tools.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeOnStack, decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversion()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var decl_src = VariableDefiniton.CreateStatement("foo", NameFactory.IntTypeReference(), initValue: IntLiteral.Create("3"));
            var decl_dst = VariableDefiniton.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { decl_src, decl_dst, Tools.Readout("bar") })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ImplicitPointerReferenceConversion()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var decl_src = VariableDefiniton.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDefiniton.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { decl_src, decl_dst, Tools.Readout("bar") })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversionOnCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var main_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create( IntLiteral.Create("5")))
                })));
            var foo_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    ExpressionFactory.Readout("x"),
                }))
                .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }
    }
}