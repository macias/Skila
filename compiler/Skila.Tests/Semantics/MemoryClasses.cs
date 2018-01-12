﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Flow;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class MemoryClasses
    {
        [TestMethod]
        public IErrorReporter ErrorViolatingAssociatedReference()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition first_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .Modifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("y", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("x", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead));

            FunctionDefinition second_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .Modifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), ExpressionReadMode.CannotBeRead));

            VariableDeclaration first_field = VariableDeclaration.CreateStatement("a", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                    Undef.Create(), EntityModifier.Reassignable | EntityModifier.Public);

            VariableDeclaration second_field = VariableDeclaration.CreateStatement("b", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                    Undef.Create(), EntityModifier.Public);

            TypeDefinition type = root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .Modifier(EntityModifier.Base | EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(first_field)
                .With(second_field)
                .With(first_constructor)
                .With(second_constructor));

            NameReference parameter_typename = NameFactory.IntTypeReference();
            root_ns.AddBuilder(TypeBuilder.Create("HelloValue")
                .Modifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .Modifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("x", parameter_typename, ExpressionReadMode.CannotBeRead))));

            FunctionParameter variadic_param = FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                        Variadic.Create(2, 5), null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            root_ns.AddBuilder(TypeBuilder.Create("HelloVariadic")
                .Modifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .Modifier(EntityModifier.UnchainBase)
                    .Parameters(variadic_param)));

            FunctionParameter optional_param = FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                        Variadic.None, IntLiteral.Create("0"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            root_ns.AddBuilder(TypeBuilder.Create("HelloOptional")
                .Modifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .Modifier(EntityModifier.UnchainBase)
                    .Parameters(optional_param)));

            VariableDeclaration value_decl = VariableDeclaration.CreateStatement("v", null, 
                ExpressionFactory.StackConstructor("Hi", IntLiteral.Create("3")));

            root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    value_decl,
                    ExpressionFactory.Readout("v")
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(9, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSealedType, type.Modifier));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleParameter, first_constructor));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleConstructor, second_constructor));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceFieldCannotBeReassignable, first_field));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleReferenceField, second_field));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresReferenceParameter, parameter_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresNonVariadicParameter, variadic_param));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresNonOptionalParameter, optional_param));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresPassingByReference, value_decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorEscapingReference()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .With(FunctionBuilder.Create("give", NameFactory.UnitTypeReference(), Block.CreateStatement())));

            Return ret = Return.Create(ExpressionFactory.StackConstructor("Hi"));
            root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                ExpressionReadMode.OptionalUse,
                NameFactory.ReferenceTypeReference("Hi"),

                Block.CreateStatement(
                    ret
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, ret.Expr));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingHeapMethodOnValue()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .With(FunctionBuilder.Create("give", NameFactory.UnitTypeReference(), Block.CreateStatement())
                    .Modifier(EntityModifier.HeapOnly)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("v", "give"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("v", null, ExpressionFactory.StackConstructor("Hi")),
                    call
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CallingHeapFunctionWithValue, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorPersistentReferenceType()
        {
            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                GlobalVariables = true,
                TypelessVariablesDuringTests = true
            });
            var root_ns = env.Root;

            var decl1 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Public);
            root_ns.AddNode(decl1);

            var decl2 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Static);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    decl2,
                    ExpressionFactory.Readout("bar")
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
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .Modifier(EntityModifier.HeapOnly)
                .Parents(NameFactory.ObjectTypeReference()));

            var decl = VariableDeclaration.CreateStatement("bar", NameReference.Create("Hi"),
                Undef.Create());

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeOnStack, decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversion()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.IntTypeReference(), initValue: IntLiteral.Create("3"));
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ImplicitPointerReferenceConversion()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversionOnCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create( IntLiteral.Create("5")))
                })));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                    usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}