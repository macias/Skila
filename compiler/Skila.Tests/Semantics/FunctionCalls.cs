using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionCalls
    {
        [TestMethod]
        public IErrorReporter RegularFunctionWithSpreadCall()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("sum"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference(NameFactory.IntTypeReference())),
                    Variadic.None, null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("sum"), Spread.Create(NameReference.Create("x")));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Return.Create(call)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter VariadicFunctionWithMixedFormArguments()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("sum"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.IntTypeReference(), Variadic.Create(),
                    null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("sum"),
                         Spread.Create(NameReference.Create("x")), IntLiteral.Create("77"));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Return.Create(call)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter VariadicFunctionMissingSpread()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("sum"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(Undef.Create())
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.IntTypeReference(), Variadic.Create(),
                    null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("sum"), NameReference.Create("x"));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Return.Create(call)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnqualifiedBaseConstructorCall()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition base_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                new[] { FunctionParameter.Create("g", NameFactory.IntTypeReference(), usageMode: ExpressionReadMode.CannotBeRead) },
                Block.CreateStatement());
            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(base_constructor));

            // without pinning down the target constructor with "base" it is not available
            FunctionCall base_call = ExpressionFactory.ThisInit(FunctionArgument.Create(IntLiteral.Create("3")));
            FunctionDefinition next_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                Block.CreateStatement(),
                base_call);

            TypeDefinition next_type = root_ns.AddBuilder(TypeBuilder.Create("Next")
                .Parents("Point")
                .Modifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(next_constructor));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, base_call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAmbiguousCallWithDistinctOutcomeTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var func1 = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("go"),
                ExpressionReadMode.OptionalUse, NameFactory.BoolTypeReference(),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));
            var func2 = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("go"),
                ExpressionReadMode.OptionalUse, NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));

            var call = root_ns.AddNode(FunctionCall.Create(NameReference.Create("go")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.OverloadingDuplicateFunctionDefinition, func1)
                || resolver.ErrorManager.HasError(ErrorCode.OverloadingDuplicateFunctionDefinition, func2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicStaticMethodCall()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var func_def = FunctionBuilder.Create(
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) }))
                .Modifier(EntityModifier.Static)
                .Build();

            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo").With(func_def));

            var call = FunctionCall.Create(NameReference.Create(NameReference.Create("Foo"), "foo"));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCall()
        {
            var env = Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                GlobalVariables = true,
                TypelessVariablesDuringTests = true
            });
            var root_ns = env.Root;

            FunctionDefinition func_def = FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                .With(func_def));

            FunctionDefinition cons_def = FunctionBuilder.Create(NameDefinition.Create("cons"),
                ExpressionReadMode.OptionalUse,
                NameReference.Create("Foo"),
                Block.CreateStatement(new IExpression[] {
                    // just playing with declaration-expression
                    Assignment.CreateStatement(NameReference.Sink(),
                        VariableDeclaration.CreateExpression("result", NameReference.Create("Foo"),initValue: Undef.Create())),
                    Return.Create(NameReference.Create("result"))
                }));

            root_ns.AddNode(cons_def);

            var cons = FunctionCall.Create(NameReference.Create("cons"));
            var call = FunctionCall.Create(NameReference.Create(cons, "foo"));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCallViaPointer()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                .With(func_def));

            var call = FunctionCall.Create(NameReference.Create(NameReference.Create("y"), "foo"));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("y", NameFactory.PointerTypeReference(NameReference.Create("Foo")),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(),
                        call),
                    ExpressionFactory.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnchainedBase()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("51"))
                    }))
                    .Modifier(EntityModifier.Base)));

            NameReference super_function_reference = NameReference.Create(NameFactory.SuperFunctionName);
            root_ns.AddBuilder(TypeBuilder.Create("End")
                .Parents("Middle")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(super_function_reference))
                    }))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase)));

            FunctionDefinition func = FunctionBuilder.Create(
                NameDefinition.Create("getB"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("1"))
                }))
            .Modifier(EntityModifier.Override);

            root_ns.AddBuilder(TypeBuilder.Create("Alter")
                .Parents("Middle")
                .With(func));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SuperCallWithUnchainedBase, super_function_reference));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DerivationWithoutSuperCall, func));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingSuperFunctionByFunctioName()
        {
            var env = Environment.Create(new Options() { ReferencingBase = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("51"))
                    }))
                    .Modifier(EntityModifier.Base)));

            NameReference super_function_reference = NameReference.Create(NameFactory.BaseVariableName, "getB");
            root_ns.AddBuilder(TypeBuilder.Create("End")
                .Parents("Middle")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(FunctionCall.Create(super_function_reference))
                    }))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NamedRecursiveReference, super_function_reference));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingSelfFunctionByFunctioName()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference self_function_reference = NameReference.Create("foo");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(FunctionCall.Create(self_function_reference)) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NamedRecursiveReference, self_function_reference));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorBasicFunctionCall()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1, EntityModifier.Public));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.IntTypeReference(),
                call2, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, call2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingNonFunction()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference non_func_ref = NameReference.Create("i");
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                    FunctionCall.Create(non_func_ref)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NotFunctionType, non_func_ref));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter MultipleVariadicParameters()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, false,
                usageMode: ExpressionReadMode.CannotBeRead);
            var param2 = FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.Create(0, null), null,
                isNameRequired: true, usageMode: ExpressionReadMode.CannotBeRead);
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(param1, param2));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var arg1 = FunctionArgument.Create(NameReference.Create("i"));
            var arg2 = FunctionArgument.Create(NameReference.Create("i"));
            var arg3 = FunctionArgument.Create("y", NameReference.Create("i"));
            var arg4 = FunctionArgument.Create(NameReference.Create("i"));
            var call = FunctionCall.Create(NameReference.Create("foo"), arg1, arg2, arg3, arg4);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(param1, arg1.MappedTo);
            Assert.AreEqual(param1, arg2.MappedTo);
            Assert.AreEqual(param2, arg3.MappedTo);
            Assert.AreEqual(param2, arg4.MappedTo);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ArbitraryOptional()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None,
                IntLiteral.Create("1"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            var param2 = FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.None,
                IntLiteral.Create("2"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                new[] { param1, param2 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var arg2 = FunctionArgument.Create("y", NameReference.Create("i"));
            // we skip over the first param and pass argument only for the second one
            var call = FunctionCall.Create(NameReference.Create("foo"), arg2);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);
            Assert.AreEqual(param2, arg2.MappedTo);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter NonOptionalVariadicParameters()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(0, null), null,
                isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                new[] { param1 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), null, EntityModifier.Public));
            var call = FunctionCall.Create(NameReference.Create("foo"));
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TargetFunctionNotFound, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InvalidNumberForVariadicParameter()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(3, 5), null,
                isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(param1));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidNumberVariadicArguments, call1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidNumberVariadicArguments, call4));

            foreach (var arg in call1.Arguments.Concat(call2.Arguments).Concat(call3.Arguments).Concat(call4.Arguments))
                Assert.AreEqual(param1, arg.MappedTo);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DuplicateArgumentsCall()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), usageMode: ExpressionReadMode.CannotBeRead),
                    FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.None, IntLiteral.Create("3"), false,
                        usageMode: ExpressionReadMode.CannotBeRead)));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var dup_arg = FunctionArgument.Create("x", NameReference.Create("i"));
            var call = FunctionCall.Create(NameReference.Create("foo"),
                FunctionArgument.Create("x", NameReference.Create("i")),
                dup_arg);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ArgumentForFunctionAlreadyGiven, dup_arg));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DirectArgumentMapping()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("a", NameFactory.IntTypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.Arguments[0]);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctorArgumentMapping()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("w", NameFactory.IntTypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));

            var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null, NameReference.Create("foo")),
                    // x Int 
                    VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                    // x Double = fooer(i)
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    // _ = x
                    ExpressionFactory.Readout("x")
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.Arguments[0]);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperMethodCallTypeInference()
        {
            Environment env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                new[] { FunctionParameter.Create("b", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Modifier(EntityModifier.Static)
                .Build();
            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo", "T").With(func_def));

            FunctionCall call = FunctionCall.Create(NameReference.Create(NameReference.Create("f"), "foo"),
                FunctionArgument.Create(IntLiteral.Create("2")));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f",
                        NameReference.Create("Foo", NameFactory.IntTypeReference()), initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    ExpressionFactory.Readout("f"),
                    ExpressionFactory.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperFunctionCallTypeInference()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("b", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.Arguments[0]);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateDirectArgumentMapping()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("d", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.IntTypeReference()),
                FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.Arguments[0]);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateFunctorArgumentMapping()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo",
                TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                    new[] { FunctionParameter.Create("e", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3"))
                })));


            var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null,
                        NameReference.Create("foo", NameFactory.IntTypeReference())),
                    // i Int 
                    VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                    // x = fooer(i)
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    // _ = x
                    ExpressionFactory.Readout("x")
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArg(call.Arguments[0]);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAmbiguousTemplateFunction()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo",
                TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                    new[] { FunctionParameter.Create("e", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3"))
                })));


            NameReference function_reference = NameReference.Create("foo");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    // fooer = foo
                    VariableDeclaration.CreateStatement("fooer", null, function_reference),
                    // _ = fooer
                    ExpressionFactory.Readout("fooer")
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelectingAmbiguousTemplateFunction, function_reference));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateResultType()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo", TemplateParametersBuffer.Create()
                .Add("T", VarianceMode.None)
                .Add("R", VarianceMode.None).Values),
                ExpressionReadMode.OptionalUse,
                NameReference.Create("R"),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("result", NameReference.Create("R"), initValue: Undef.Create()),
                    Return.Create(NameReference.Create("result"))
                }))
                .Parameters(FunctionParameter.Create("q", NameReference.Create("T"), Variadic.None, null, false,
                    usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.IntTypeReference(), NameFactory.DoubleTypeReference()),
                FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.DoubleType.InstanceOf, call.Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CallingFunctionWithOptionalParameters()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) }))
                .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None,
                    defaultValue: IntLiteral.Create("1"), isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));
            var call = FunctionCall.Create(NameReference.Create("foo"));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }
    }
}