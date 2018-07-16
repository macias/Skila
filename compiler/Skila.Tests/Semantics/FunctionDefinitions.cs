using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionDefinitions
    {
        [TestMethod]
        public IErrorReporter ErrorInvalidMainResultType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference result_typename = NameFactory.UnitNameReference();
                root_ns.AddBuilder(FunctionBuilder.Create(
                    NameFactory.MainFunctionName,
                    ExpressionReadMode.OptionalUse,
                    result_typename,

                    Block.CreateStatement()));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MainFunctionInvalidResultType, result_typename));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInvalidConverters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition conv1 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                     NameFactory.Int64NameReference(),
                        Block.CreateStatement(Return.Create(Int64Literal.Create("3"))));
                FunctionDefinition conv2 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                     ExpressionReadMode.OptionalUse,
                     NameFactory.BoolNameReference(),
                        Block.CreateStatement(Return.Create(Undef.Create())))
                        .SetModifier(EntityModifier.Pinned);
                FunctionDefinition conv3 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                     NameFactory.StringPointerNameReference(),
                        Block.CreateStatement(Return.Create(Undef.Create())))
                        .SetModifier(EntityModifier.Pinned)
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead));

                root_ns.AddBuilder(TypeBuilder.Create("Start")
                    .SetModifier(EntityModifier.Base)
                    .With(conv1)
                    .With(conv2)
                    .With(conv3));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterNotPinned, conv1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterDeclaredWithIgnoredOutput, conv2));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterWithParameters, conv3));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCannotInferResultType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression lambda = FunctionBuilder.CreateLambda(null,
                        Block.CreateStatement(new IExpression[] {
                        IfBranch.CreateIf(BoolLiteral.CreateFalse(),new[]{
                            Return.Create(BoolLiteral.CreateTrue())
                        }),
                        Return.Create(Int64Literal.Create("2"))
                        })).Build();
                root_ns.AddBuilder(FunctionBuilder.Create("me",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    // f = () => x
                    VariableDeclaration.CreateStatement("f",null,lambda),
                    ExpressionFactory.Readout("f")
                    })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotInferResultType, lambda));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperReturning()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("5")) })));
                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foox",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] { Return.Create() })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorReturning()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Return empty_return = Return.Create();
                var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] { empty_return })));
                Int64Literal return_value = Int64Literal.Create("5");
                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foox",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] { Return.Create(return_value) })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EmptyReturn, empty_return));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, return_value));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AnonymousVariadicParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(), null,
                    isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                // the tail variadic parameter has to require name
                var param2 = FunctionParameter.Create("y", NameFactory.Int64NameReference(), Variadic.Create(), null,
                    isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                    })).Parameters(param1, param2));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), null, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AnonymousTailVariadicParameter, param2));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorVariadicParametersInvalidLimits()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(4, 3), null,
                    isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                    }))
                    .Parameters(param1));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), null, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidVariadicLimits, param1));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter NonConflictingOverload()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                   "foo",
                   ExpressionReadMode.OptionalUse,
                   NameFactory.RealNameReference(),
                   Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                       .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddBuilder(FunctionBuilder.Create(
                   "foo",
                   ExpressionReadMode.OptionalUse,
                   NameFactory.RealNameReference(),
                   Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                       .Parameters(FunctionParameter.Create("x", NameFactory.RealNameReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingVariadicOverload()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(1, 3),
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(3, 5),
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorConflictingOverlappingOptionalOverload()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                            usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddBuilder(FunctionBuilder.Create(
                   "foo",
                   ExpressionReadMode.OptionalUse,
                   NameFactory.RealNameReference(),
                   Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                       .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, Int64Literal.Create("3"), false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDefaultUndef()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Undef default_value = Undef.Create();
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, default_value, false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InitializationWithUndef, default_value));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingAdditionalOptionalOverload()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) })));
                var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) })).Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(),
                        Variadic.None, Int64Literal.Create("3"), false, usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateSpecializationOverload()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", "T", VarianceMode.None,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddBuilder(FunctionBuilder.Create(
                   "foo",
                    ExpressionReadMode.OptionalUse,
                   NameFactory.RealNameReference(),
                   Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                       .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionParameter parameter = FunctionParameter.Create("x", NameFactory.Int64NameReference());
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(parameter));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, parameter.Name));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingDisabledParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference param_ref = NameReference.Create("x");
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(param_ref)
                    }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.RealNameReference(), ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, param_ref));
            }

            return resolver;
        }
    }
}