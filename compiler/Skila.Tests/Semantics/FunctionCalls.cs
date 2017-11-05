using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionCalls
    {
        [TestMethod]
        public IErrorReporter ErrorAmbiguousCallWithDistinctOutcomeTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var func1 = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("go"),
                null, ExpressionReadMode.OptionalUse, NameFactory.BoolTypeReference(),
                Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) })));
            var func2 = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("go"),
                null, ExpressionReadMode.OptionalUse, NameFactory.IntTypeReference(),
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
            var env = Environment.Create();
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
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                .With(func_def));

            var cons_def = FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("cons"), Enumerable.Empty<FunctionParameter>(),
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
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicInstanceMethodCallViaPointer()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] { Return.Create(BoolLiteral.CreateTrue()) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                .With(func_def));

            var call = FunctionCall.Create(NameReference.Create(NameReference.Create("y"), "foo"));
            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("wrapper"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("y", NameFactory.PointerTypeReference(NameReference.Create("Foo")),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(),
                        call),
                    Tools.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorBasicFunctionCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.IntTypeReference(),
                call2));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(call2, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingNonFunction()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference non_func_ref = NameReference.Create("i");
            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
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
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, false);
            var param2 = FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, isNameRequired: true);
            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { param1, param2 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var arg1 = FunctionArgument.Create(NameReference.Create("i"));
            var arg2 = FunctionArgument.Create(NameReference.Create("i"));
            var arg3 = FunctionArgument.Create("y", NameReference.Create("i"));
            var arg4 = FunctionArgument.Create(NameReference.Create("i"));
            var call = FunctionCall.Create(NameReference.Create("foo"), arg1, arg2, arg3, arg4);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(param1, arg1.MappedTo);
            Assert.AreEqual(param1, arg2.MappedTo);
            Assert.AreEqual(param2, arg3.MappedTo);
            Assert.AreEqual(param2, arg4.MappedTo);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ArbitraryOptional()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None,
                IntLiteral.Create("1"), isNameRequired: false);
            var param2 = FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.None,
                IntLiteral.Create("2"), isNameRequired: false);
            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { param1, param2 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var arg2 = FunctionArgument.Create("y", NameReference.Create("i"));
            // we skip over the first param and pass argument only for the second one
            var call = FunctionCall.Create(NameReference.Create("foo"), arg2);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);
            Assert.AreEqual(param2, arg2.MappedTo);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter NonOptionalVariadicParameters()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, isNameRequired: false);
            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { param1 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), null));
            var call = FunctionCall.Create(NameReference.Create("foo"));
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.TargetFunctionNotFound, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(call, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InvalidNumberForVariadicParameter()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(3, 5), null, isNameRequired: false);
            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { param1 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

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

            foreach (var arg in call1.Arguments.Concat(call2.Arguments).Concat(call3.Arguments).Concat(call4.Arguments))
                Assert.AreEqual(param1, arg.MappedTo);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count());
            Assert.IsTrue(resolver.ErrorManager.Errors.All(it => it.Code == ErrorCode.InvalidNumberVariadicArguments));
            Assert.AreEqual(call1, resolver.ErrorManager.Errors[0].Node);
            Assert.AreEqual(call4, resolver.ErrorManager.Errors[1].Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DuplicateArgumentsCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None,null, false),
                FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.None,IntLiteral.Create("3"), false)},
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var dup_arg = FunctionArgument.Create("x", NameReference.Create("i"));
            var call = FunctionCall.Create(NameReference.Create("foo"),
                FunctionArgument.Create("x", NameReference.Create("i")),
                dup_arg);
            root_ns.AddNode(call);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ArgumentForFunctionAlreadyGiven, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(dup_arg, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DirectArgumentMapping()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("a", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArgIndex(0);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctorArgumentMapping()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("w", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("wrapper"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("fooer", null, NameReference.Create("foo")),
                    VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    Tools.Readout("x")
        })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArgIndex(0);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperMethodCallTypeInference()
        {
            Environment env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_def = FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                new[] { FunctionParameter.Create("b", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) }))
                .Modifier(EntityModifier.Static)
                .Build();
            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo", "T").With(func_def));

            FunctionCall call = FunctionCall.Create(NameReference.Create(NameReference.Create("f"), "foo"),
                FunctionArgument.Create(IntLiteral.Create("2")));
            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("wrapper"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f",
                        NameReference.Create("Foo", NameFactory.IntTypeReference()), initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    Tools.Readout("f"),
                    Tools.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperFunctionCallTypeInference()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                new[] { FunctionParameter.Create("b", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArgIndex(0);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateDirectArgumentMapping()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                new[] { FunctionParameter.Create("d", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.IntTypeReference()),
                FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArgIndex(0);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateFunctorArgumentMapping()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo", TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values),
                new[] { FunctionParameter.Create("e", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));


            var call = FunctionCall.Create(NameReference.Create("fooer"), FunctionArgument.Create(NameReference.Create("i")));

            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("wrapper"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("fooer", null,
                        NameReference.Create("foo", NameFactory.IntTypeReference())),
                    VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                    VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                        call),
                    Tools.Readout("x")
        })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            IEntityInstance param_eval = call.Resolution.GetTransParamEvalByArgIndex(0);
            Assert.AreEqual(env.IntType.InstanceOf, param_eval);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateResultType()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo", TemplateParametersBuffer.Create()
                .Add("T", VarianceMode.None)
                .Add("R", VarianceMode.None).Values),
                new[] { FunctionParameter.Create("q", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameReference.Create("R"),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("result", NameReference.Create("R"), initValue: Undef.Create()),
                    Return.Create(NameReference.Create("result"))
                })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo", NameFactory.IntTypeReference(), NameFactory.DoubleTypeReference()),
                FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.DoubleType.InstanceOf, call.Evaluation);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter CallingFunctionWithOptionalParameters()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None,
                defaultValue: IntLiteral.Create("1"), isNameRequired: false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var call = FunctionCall.Create(NameReference.Create("foo"));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def, call.Resolution.TargetInstance.Target);

            return resolver;
        }
    }
}