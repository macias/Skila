using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;
using Skila.Language.Comparers;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionCalls : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorRegularFunctionWithSpreadCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.ReferenceNameReference(NameFactory.ISequenceNameReference(NameFactory.Int64NameReference())),
                        Variadic.None, null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("sum"), Spread.Create(NameReference.Create("x")));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                         ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Return.Create(call)
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter VariadicFunctionWithMixedFormArguments()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64NameReference(), Variadic.Create(),
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("sum"),
                             Spread.Create(NameReference.Create("x")), Int64Literal.Create("77"));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                         ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Return.Create(call)
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));
            }
            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorVariadicFunctionMissingSpread()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64NameReference(), Variadic.Create(),
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("sum"), NameReference.Create("x"));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                         ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Return.Create(call)
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnqualifiedBaseConstructorCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition base_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                    new[] { FunctionParameter.Create("g", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead) },
                    Block.CreateStatement());
                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable | EntityModifier.Base)
                    .With(base_constructor));

                // without pinning down the target constructor with "base" it is not available
                FunctionCall base_call =  ExpressionFactory.ThisInit(FunctionArgument.Create(Int64Literal.Create("3")));
                FunctionDefinition next_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                    Block.CreateStatement(),
                    base_call);

                TypeDefinition next_type = root_ns.AddBuilder(TypeBuilder.Create("Next")
                    .Parents("Point")
                    .SetModifier(EntityModifier.Mutable | EntityModifier.Base)
                    .With(next_constructor));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, base_call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAmbiguousCallWithDistinctOutcomeTypes()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var func1 = root_ns.AddBuilder(FunctionBuilder.Create("go",
                    ExpressionReadMode.OptionalUse, NameFactory.BoolNameReference(),
                    Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));
                var func2 = root_ns.AddBuilder(FunctionBuilder.Create("go",
                    ExpressionReadMode.OptionalUse, NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));

                var call = root_ns.AddNode(FunctionCall.Create(NameReference.Create("go")));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.OverloadingDuplicateFunctionDefinition, func1)
                    || resolver.ErrorManager.HasError(ErrorCode.OverloadingDuplicateFunctionDefinition, func2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicStaticMethodCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def = FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) }))
                    .SetModifier(EntityModifier.Static)
                    .Build();

                var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo").With(func_def));

                var call = FunctionCall.Create(NameReference.Create(NameReference.Create("Foo"), "foo"));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                    .With(func_def));

                FunctionDefinition cons_def = FunctionBuilder.Create("cons",
                    ExpressionReadMode.OptionalUse,
                    NameReference.Create("Foo"),
                    Block.CreateStatement(new IExpression[] {
                    // just playing with declaration-expression
                     ExpressionFactory.Readout(VariableDeclaration.CreateExpression("result", NameReference.Create("Foo"),initValue: Undef.Create())),
                    Return.Create(NameReference.Create("result"))
                    }));

                root_ns.AddNode(cons_def);

                var cons = FunctionCall.Create(NameReference.Create("cons"));
                var call = FunctionCall.Create(NameReference.Create(cons, "foo"));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.BoolNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCallViaPointer()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

                var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                    .With(func_def));

                var call = FunctionCall.Create(NameReference.Create(NameReference.Create("y"), "foo"));
                root_ns.AddBuilder(FunctionBuilder.Create("wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("y", NameFactory.PointerNameReference(NameReference.Create("Foo")),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.BoolNameReference(),
                        call),
                     ExpressionFactory.Readout("x")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnchainedBase()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Middle")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("51"))
                        }))
                        .SetModifier(EntityModifier.Base)));

                NameReference super_function_reference = NameReference.Create(NameFactory.SuperFunctionName);
                root_ns.AddBuilder(TypeBuilder.Create("End")
                    .Parents("Middle")
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(super_function_reference))
                        }))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase)));

                FunctionDefinition func = FunctionBuilder.Create(
                    "getB",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("1"))
                    }))
                .SetModifier(EntityModifier.Override);

                root_ns.AddBuilder(TypeBuilder.Create("Alter")
                    .Parents("Middle")
                    .With(func));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SuperCallWithUnchainedBase, super_function_reference));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DerivationWithoutSuperCall, func));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingSuperFunctionByFunctioName()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { ReferencingBase = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Middle")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("51"))
                        }))
                        .SetModifier(EntityModifier.Base)));

                NameReference super_function_reference = NameReference.Create(NameFactory.BaseVariableName, "getB");
                root_ns.AddBuilder(TypeBuilder.Create("End")
                    .Parents("Middle")
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(super_function_reference))
                        }))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NamedRecursiveFunctionReference, super_function_reference));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingSelfFunctionByFunctioName()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference self_function_reference = NameReference.Create("foo");
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(FunctionCall.Create(self_function_reference)) })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NamedRecursiveFunctionReference, self_function_reference));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorBasicFunctionCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead)));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call1, EntityModifier.Public));
                root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.Int64NameReference(),
                    call2, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, call2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingNonFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference non_func_ref = NameReference.Create("i");
                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()),
                    FunctionCall.Create(non_func_ref)
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NotFunctionType, non_func_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter MultipleVariadicParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(), null, false,
                    usageMode: ExpressionReadMode.CannotBeRead);
                var param2 = FunctionParameter.Create("y", NameFactory.Int64NameReference(), Variadic.Create(), null,
                    isNameRequired: true, usageMode: ExpressionReadMode.CannotBeRead);
                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(param1, param2));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var arg1 = FunctionArgument.Create(NameReference.Create("i"));
                var arg2 = FunctionArgument.Create(NameReference.Create("i"));
                var arg3 = FunctionArgument.Create("y", NameReference.Create("i"));
                var arg4 = FunctionArgument.Create(NameReference.Create("i"));
                var call = FunctionCall.Create(NameReference.Create("foo"), arg1, arg2, arg3, arg4);
                root_ns.AddNode(call);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(param1, arg1.MappedTo);
                Assert.AreEqual(param1, arg2.MappedTo);
                Assert.AreEqual(param2, arg3.MappedTo);
                Assert.AreEqual(param2, arg4.MappedTo);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ArbitraryOptional()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None,
                    Int64Literal.Create("1"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                var param2 = FunctionParameter.Create("y", NameFactory.Int64NameReference(), Variadic.None,
                    Int64Literal.Create("2"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(param1, param2));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var arg2 = FunctionArgument.Create("y", NameReference.Create("i"));
                // we skip over the first param and pass argument only for the second one
                var call = FunctionCall.Create(NameReference.Create("foo"), arg2);
                root_ns.AddNode(call);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
                Assert.AreEqual(param2, arg2.MappedTo);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter NonOptionalVariadicParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(), null,
                    isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) })).Parameters(param1));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), null, EntityModifier.Public));
                var call = FunctionCall.Create(NameReference.Create("foo"));
                root_ns.AddNode(call);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InvalidNumberForVariadicParameter()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var param1 = FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.Create(3, 6), null,
                    isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(param1));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call1 = FunctionCall.Create(NameReference.Create("foo"),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")));
                var call2 = FunctionCall.Create(NameReference.Create("foo"),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")));
                var call3 = FunctionCall.Create(NameReference.Create("foo"),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")));
                var call4 = FunctionCall.Create(NameReference.Create("foo"),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")),
                    FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(call1);
                root_ns.AddNode(call2);
                root_ns.AddNode(call3);
                root_ns.AddNode(call4);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidNumberVariadicArguments, call1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidNumberVariadicArguments, call4));

                foreach (var arg in call1.UserArguments.Concat(call2.UserArguments).Concat(call3.UserArguments).Concat(call4.UserArguments))
                    Assert.AreEqual(param1, arg.MappedTo);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DuplicateArgumentsCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("y", NameFactory.Int64NameReference(), Variadic.None, Int64Literal.Create("3"), false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var dup_arg = FunctionArgument.Create("x", NameReference.Create("i"));
                var call = FunctionCall.Create(NameReference.Create("foo"),
                    FunctionArgument.Create("x", NameReference.Create("i")),
                    dup_arg);
                root_ns.AddNode(call);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ArgumentForFunctionAlreadyGiven, dup_arg));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DirectArgumentMapping()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("a", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.UserArguments[0]);
                Assert.IsTrue(env.Int64Type.InstanceOf.HasSameCore(param_eval));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctorArgumentMapping()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("w", NameFactory.Int64NameReference(), usageMode: ExpressionReadMode.CannotBeRead)));

                var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

                root_ns.AddBuilder(FunctionBuilder.Create("wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null, NameReference.Create("foo")),
                    // x Int 
                    VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()),
                    // x Double = fooer(i)
                    VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                        call),
                    // _ = x
                     ExpressionFactory.Readout("x")
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.UserArguments[0]);
                Assert.AreEqual(env.Int64Type.InstanceOf, param_eval);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperMethodCallTypeInference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                Environment env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .SetModifier(EntityModifier.Static)
                    .Parameters(FunctionParameter.Create("b", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead));

                var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo", "T").With(func_def));

                FunctionCall call = FunctionCall.Create(NameReference.Create(NameReference.Create("f"), "foo"),
                    FunctionArgument.Create(Int64Literal.Create("2")));
                root_ns.AddBuilder(FunctionBuilder.Create("wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f",
                        NameReference.Create("Foo", NameFactory.Int64NameReference()), initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                        call),
                     ExpressionFactory.Readout("f"),
                     ExpressionFactory.Readout("x")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperFunctionCallTypeInference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("b", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.UserArguments[0]);
                Assert.IsTrue(env.Int64Type.InstanceOf.HasSameCore(param_eval));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateDirectArgumentMapping()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("d", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.Int64NameReference()),
                    FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.UserArguments[0]);
                Assert.AreEqual(env.Int64Type.InstanceOf, param_eval);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateFunctorArgumentMapping()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                    })).Parameters(FunctionParameter.Create("e", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));


                var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

                root_ns.AddBuilder(FunctionBuilder.Create("wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null,
                        NameReference.Create("foo", NameFactory.Int64NameReference())),
                    // i Int 
                    VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()),
                    // x = fooer(i)
                    VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                        call),
                    // _ = x
                     ExpressionFactory.Readout("x")
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.UserArguments[0]);
                Assert.AreEqual(env.Int64Type.InstanceOf, param_eval);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAmbiguousTemplateFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                    }))
                    .Parameters(FunctionParameter.Create("e", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));


                NameReference function_reference = NameReference.Create("foo");
                root_ns.AddBuilder(FunctionBuilder.Create("wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null, function_reference),
                    // _ = fooer
                     ExpressionFactory.Readout("fooer")
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelectingAmbiguousTemplateFunction, function_reference));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateResultType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo", TemplateParametersBuffer.Create()
                    .Add("T", VarianceMode.None)
                    .Add("R", VarianceMode.None).Values,
                    ExpressionReadMode.OptionalUse,
                    NameReference.Create("R"),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("result", NameReference.Create("R"), initValue: Undef.Create()),
                    Return.Create(NameReference.Create("result"))
                    }))
                    .Parameters(FunctionParameter.Create("q", NameReference.Create("T"), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64NameReference(), Undef.Create()));
                var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.Int64NameReference(), NameFactory.RealNameReference()),
                    FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(env.Real64Type.InstanceOf, call.Evaluation.Components);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CallingFunctionWithOptionalParameters()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None,
                        defaultValue: Int64Literal.Create("1"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));
                var call = FunctionCall.Create(NameReference.Create("foo"));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(),
                    call, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }
    }
}