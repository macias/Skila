using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class MemoryClasses
    {
        [TestMethod]
        public IErrorReporter ErrorHeapTypeAsValue()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference param_typename = NameReference.Create("Hi");
                NameReference result_typename = NameReference.Create("Hi");
                NameReference param_it_typename = NameFactory.ItNameReference();
                NameReference result_it_typename = NameFactory.ItNameReference();

                Dereference bad_dereference = Dereference.Create(NameReference.CreateThised());

                NameReference decl_it_typename = NameFactory.ItNameReference();

                root_ns.AddBuilder(TypeBuilder.Create("Hi")
                    .SetModifier(EntityModifier.HeapOnly)
                    .Parents(NameFactory.IObjectNameReference())

                    .With(FunctionBuilder.Create("named", result_typename,
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("n", null, bad_dereference),
                        Return.Create(NameReference.Create("n"))))
                    .Parameters(FunctionParameter.Create("p", param_typename, ExpressionReadMode.CannotBeRead)))

                    .With(FunctionBuilder.Create("itted", result_it_typename,
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("it", decl_it_typename, Undef.Create()),
                        Return.Create(NameReference.Create("it"))))
                    .Parameters(FunctionParameter.Create("p", param_it_typename, ExpressionReadMode.CannotBeRead)))
                    );

                var decl = VariableDeclaration.CreateStatement("bar", NameReference.Create("Hi"),
                    Undef.Create());

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("bar")
                    })));

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, decl.TypeName));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, result_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, param_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, result_it_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, param_it_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, bad_dereference));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, decl_it_typename));
                Assert.AreEqual(7, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorViolatingAssociatedReference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition first_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                        .SetModifier(EntityModifier.UnchainBase)
                        .Parameters(FunctionParameter.Create("y", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead),
                            FunctionParameter.Create("x", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead));

                FunctionDefinition second_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                        .SetModifier(EntityModifier.UnchainBase)
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead));

                VariableDeclaration first_field = VariableDeclaration.CreateStatement("a", 
                    NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                    Undef.Create(), env.Options.ReassignableModifier() | EntityModifier.Public);

                VariableDeclaration second_field = VariableDeclaration.CreateStatement("b", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                        Undef.Create(), EntityModifier.Public);

                TypeDefinition type = root_ns.AddBuilder(TypeBuilder.Create("Hi")
                    .SetModifier(EntityModifier.Base | EntityModifier.AssociatedReference | EntityModifier.Mutable)
                    .With(first_field)
                    .With(second_field)
                    .With(first_constructor)
                    .With(second_constructor));

                NameReference parameter_typename = NameFactory.Int64NameReference();
                root_ns.AddBuilder(TypeBuilder.Create("HelloValue")
                    .SetModifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                        .SetModifier(EntityModifier.UnchainBase)
                        .Parameters(FunctionParameter.Create("x", parameter_typename, ExpressionReadMode.CannotBeRead))));

                FunctionParameter variadic_param = FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                            Variadic.Create(2, 6), null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                root_ns.AddBuilder(TypeBuilder.Create("HelloVariadic")
                    .SetModifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                        .SetModifier(EntityModifier.UnchainBase)
                        .Parameters(variadic_param)));

                FunctionParameter optional_param = FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                            Variadic.None, Int64Literal.Create("0"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                root_ns.AddBuilder(TypeBuilder.Create("HelloOptional")
                    .SetModifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                        .SetModifier(EntityModifier.UnchainBase)
                        .Parameters(optional_param)));

                VariableDeclaration value_decl = VariableDeclaration.CreateStatement("v", null,
                    ExpressionFactory.StackConstructor("Hi", Int64Literal.Create("3")));

                root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        value_decl,
                        ExpressionFactory.Readout("v")
                    )));

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSealedType, type.Modifier));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleParameter, first_constructor));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleConstructor, second_constructor));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceFieldCannotBeReassignable, first_field));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresSingleReferenceField, second_field));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresReferenceParameter, parameter_typename));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresNonVariadicParameter, variadic_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresNonOptionalParameter, optional_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssociatedReferenceRequiresPassingByReference, value_decl));
                Assert.AreEqual(9, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingHeapMethodOnValue()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Hi")
                    .With(FunctionBuilder.Create("give", NameFactory.UnitNameReference(), Block.CreateStatement())
                        .SetModifier(EntityModifier.HeapOnly)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("v", "give"));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("v", null, ExpressionFactory.StackConstructor("Hi")),
                        call
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CallingHeapFunctionWithValue, call));
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversion()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.Int64NameReference(), initValue: Int64Literal.Create("3"));
                var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                    initValue: NameReference.Create("foo"));

                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ImplicitPointerReferenceConversion()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                    initValue: Undef.Create());
                var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                    initValue: NameReference.Create("foo"));

                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversionOnCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create( Int64Literal.Create("5")))
                    })));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement())
                    .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                        usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }
    }
}