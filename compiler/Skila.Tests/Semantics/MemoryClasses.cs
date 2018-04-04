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
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            NameReference param_typename = NameReference.Create("Hi");
            NameReference result_typename = NameReference.Create("Hi");
            NameReference param_it_typename = NameFactory.ItTypeReference();
            NameReference result_it_typename = NameFactory.ItTypeReference();

            Dereference bad_dereference = Dereference.Create(NameReference.CreateThised());

            NameReference decl_it_typename = NameFactory.ItTypeReference();

            root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .SetModifier(EntityModifier.HeapOnly)
                .Parents(NameFactory.IObjectTypeReference())

                .With(FunctionBuilder.Create("named", result_typename,
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("n",null, bad_dereference),
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
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, decl.TypeName));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, result_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, param_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, result_it_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, param_it_typename));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, bad_dereference));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeAsValue, decl_it_typename));
            Assert.AreEqual(7, resolver.ErrorManager.Errors.Count);

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorReferenceEscapesFromScope()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            IExpression assignment = Assignment.CreateStatement(NameReference.Create("x"), Int64Literal.Create("3"));
            root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("x", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()), null,
                        EntityModifier.Reassignable),
                    Block.CreateStatement(
                        // escaping assignment, once we exit the scope we lose the source of the reference --> error
                        assignment
                        ),
                    ExpressionFactory.Readout("x")
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, assignment));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorViolatingAssociatedReference()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition first_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .SetModifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("y", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead));

            FunctionDefinition second_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .SetModifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead));

            VariableDeclaration first_field = VariableDeclaration.CreateStatement("a", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                    Undef.Create(), EntityModifier.Reassignable | EntityModifier.Public);

            VariableDeclaration second_field = VariableDeclaration.CreateStatement("b", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                    Undef.Create(), EntityModifier.Public);

            TypeDefinition type = root_ns.AddBuilder(TypeBuilder.Create("Hi")
                .SetModifier(EntityModifier.Base | EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(first_field)
                .With(second_field)
                .With(first_constructor)
                .With(second_constructor));

            NameReference parameter_typename = NameFactory.Int64TypeReference();
            root_ns.AddBuilder(TypeBuilder.Create("HelloValue")
                .SetModifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .SetModifier(EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("x", parameter_typename, ExpressionReadMode.CannotBeRead))));

            FunctionParameter variadic_param = FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                        Variadic.Create(2, 6), null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            root_ns.AddBuilder(TypeBuilder.Create("HelloVariadic")
                .SetModifier(EntityModifier.AssociatedReference | EntityModifier.Mutable)
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement())
                    .SetModifier(EntityModifier.UnchainBase)
                    .Parameters(variadic_param)));

            FunctionParameter optional_param = FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
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
        public IErrorReporter ErrorReferenceEscapesFromFunction()
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
                    .SetModifier(EntityModifier.HeapOnly)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("v", "give"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "notimportant",
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
                RelaxedMode = true
            });
            var root_ns = env.Root;

            var decl1 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Public);
            root_ns.AddNode(decl1);

            var decl2 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Static);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                "notimportant",
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
        public IErrorReporter ImplicitValueReferenceConversion()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.Int64TypeReference(), initValue: Int64Literal.Create("3"));
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                "notimportant",
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

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.Int64TypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                "notimportant",
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
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create( Int64Literal.Create("5")))
                })));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                    usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}