using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Templates
    {
        [TestMethod]
        public IInterpreter ResolvingGenericArgumentInRuntime()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("oracle", "O", VarianceMode.None,
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(IsType.Create(NameReference.Create("thing"), NameReference.Create("O")))
                    ))
                    .Parameters(FunctionParameter.Create("thing", NameFactory.ReferenceNameReference(NameFactory.IObjectNameReference()))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("i", null, ExpressionFactory.HeapConstructor(NameFactory.IntNameReference(), IntLiteral.Create("7"))),
                        VariableDeclaration.CreateStatement("d", null, ExpressionFactory.HeapConstructor(NameFactory.RealNameReference(), RealLiteral.Create("3.3"))),
                        IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("oracle", NameFactory.IntNameReference()),
                            NameReference.Create("i")),
                            new[] { ExpressionFactory.IncBy("acc", Nat8Literal.Create("2")) }),
                        IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("oracle", NameFactory.IntNameReference()),
                            NameReference.Create("d")),
                            new[] { ExpressionFactory.IncBy("acc", Nat8Literal.Create("88")) }),
                        Return.Create(NameReference.Create("acc"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)2, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CheckingHostTraitRuntimeType()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));


                root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .SetModifier(EntityModifier.Trait)
                    .Parents("ISay")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(
                            Return.Create(Int64Literal.Create("2"))
                        )).SetModifier(EntityModifier.Override)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        // just plain host, no trait is used
                        VariableDeclaration.CreateStatement("g", NameFactory.PointerNameReference(NameFactory.IObjectNameReference()),
                            ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")))),
                        // we should have fail-test here
                        Return.Create(ExpressionFactory.Ternary(IsType.Create(NameReference.Create("g"), NameReference.Create("ISay")),
                            Int64Literal.Create("99"), Int64Literal.Create("2")))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CheckingTraitRuntimeType()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("Say")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("7"))
                    }))
                    .SetModifier(EntityModifier.Override))
                    .Parents("ISay"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .SetModifier(EntityModifier.Trait)
                    .Parents("ISay")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(
                            Return.Create(Int64Literal.Create("2"))
                        )).SetModifier(EntityModifier.Override)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("g", NameFactory.PointerNameReference(NameFactory.IObjectNameReference()),
                            ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                        Return.Create(ExpressionFactory.Ternary(IsType.Create(NameReference.Create("g"), NameReference.Create("ISay")),
                            Int64Literal.Create("2"), Int64Literal.Create("88")))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingTraitMethodViaInterface()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("Say")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("7"))
                    }))
                    .SetModifier(EntityModifier.Override))
                    .Parents("ISay"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .SetModifier(EntityModifier.Trait)
                    .Parents("ISay")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(
                            Return.Create(Int64Literal.Create("2"))
                        )).SetModifier(EntityModifier.Override)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        // crucial point, we store our object as interface *ISay
                        VariableDeclaration.CreateStatement("g", NameFactory.PointerNameReference("ISay"),
                            ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                        // we call method "say" implemented in trait
                        Return.Create(FunctionCall.Create(NameReference.Create("g", "say")))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingTraitMethod()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("Say")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("2"))
                    }))
                    .SetModifier(EntityModifier.Override))
                    .Parents("ISay"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .SetModifier(EntityModifier.Trait)
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .With(FunctionBuilder.Create("hello", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        Return.Create(FunctionCall.Create(NameReference.Create("s", "say")))
                    ))
                    .Parameters(FunctionParameter.Create("s", NameFactory.ReferenceNameReference("X")))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("g", null,
                            ExpressionFactory.StackConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                        VariableDeclaration.CreateStatement("y", null, ExpressionFactory.StackConstructor("Say")),
                        Return.Create(FunctionCall.Create(NameReference.Create("g", "hello"), NameReference.Create("y")))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithPointer()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true,
                    AllowProtocols = true, DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference());
                root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                         ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                         }))
                         .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                         .Parameters(FunctionParameter.Create("t", NameFactory.PointerNameReference("T"), Variadic.None, null, isNameRequired: false)));

                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                    .With(FunctionBuilder.Create("getMe",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))));

                FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithValue()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    AllowInvalidMainResult = true,
                    AllowProtocols = true,
                    DebugThrowOnError = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference());
                root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                         }))
                         .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                         .Parameters(FunctionParameter.Create("t", NameReference.Create("T"))));

                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                    .With(FunctionBuilder.Create("getMe",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))));

                FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.StackConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}