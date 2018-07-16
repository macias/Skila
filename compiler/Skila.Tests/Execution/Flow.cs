using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Flow
    {
        [TestMethod]
        public IInterpreter ParallelOptionalDeclaration()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression opt_declaration = ExpressionFactory.OptionalDeclaration(new[] {
                        VariablePrototype.Create("x",NameFactory.Nat8NameReference()),
                        VariablePrototype.Create("y",NameFactory.Nat8NameReference()) }, new[] {
                            ExpressionFactory.OptionOf(NameFactory.Nat8NameReference(), Nat8Literal.Create("2")),
                            ExpressionFactory.OptionOf(NameFactory.Nat8NameReference(), Nat8Literal.Create("7")),
                        });

                root_ns.AddBuilder(FunctionBuilder.Create("main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        IfBranch.CreateIf(opt_declaration,
                                Return.Create(ExpressionFactory.Add("x", "y")),
                            IfBranch.CreateElse(
                                ExpressionFactory.GenericThrow()
                                ))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)9, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ShortcutComputationInOptionalDeclaration()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // purpose: check if RHS of the optional declaration is computed only when it is needed
                // here we count RHS computations and since we declare two variables
                // let (x,y) =? (None,Some)
                // Some should not be executed, because `x` assigment fails first
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Mutator")
                    .SetModifier(EntityModifier.Mutable)
                    .With(VariableDeclaration.CreateStatement("c", NameFactory.IntNameReference(), null,
                        env.Options.ReassignableModifier() | EntityModifier.Public)));

                // return Some or None depending on the `f` parameter, and also increments the count of option evaluations
                root_ns.AddBuilder(FunctionBuilder.Create("give", NameFactory.OptionNameReference(NameFactory.Nat8NameReference()),
                    Block.CreateStatement(
                            ExpressionFactory.Inc(() => NameReference.Create("m", "c")),
                            Return.Create(ExpressionFactory.Ternary(NameReference.Create("f"),
                                ExpressionFactory.OptionOf(NameFactory.Nat8NameReference(), Nat8Literal.Create("11")),
                                ExpressionFactory.OptionEmpty(NameFactory.Nat8NameReference())))
                        ))
                        .Parameters(FunctionParameter.Create("f", NameFactory.BoolNameReference()),
                        FunctionParameter.Create("m", NameFactory.ReferenceNameReference(NameReference.Create("Mutator")))));

                IExpression opt_declaration = ExpressionFactory.OptionalDeclaration(new[] {
                        VariablePrototype.Create("x",NameFactory.Nat8NameReference()),
                        VariablePrototype.Create("y",NameFactory.Nat8NameReference()) }, new[] {
                            FunctionCall.Create("give",BoolLiteral.CreateFalse(),NameReference.Create("mut")),
                            FunctionCall.Create("give",BoolLiteral.CreateTrue(),NameReference.Create("mut")),
                        });

                root_ns.AddBuilder(FunctionBuilder.Create("main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("mut", null, ExpressionFactory.StackConstructor(NameReference.Create("Mutator"))),

                        IfBranch.CreateIf(opt_declaration,
                            new[] {
                            ExpressionFactory.Readout("x"),
                            ExpressionFactory.Readout("y"),
                            ExpressionFactory.GenericThrow(),
                            },
                            IfBranch.CreateElse(
                                // crucial check -- we should not evaluate the second option
                                ExpressionFactory.AssertEqual(IntLiteral.Create("1"), NameReference.Create("mut", "c"))
                                )),

                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter InitializationWithOptionalAssignment()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                // this test is a bit tougher than regular opt.assignment, because variables will be 
                // initialized for the first time with this assigment
                root_ns.AddBuilder(FunctionBuilder.Create("main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), env.Options.ReassignableModifier()),


                        VariableDeclaration.CreateStatement("x", null,
                            ExpressionFactory.OptionOf(NameFactory.Nat8NameReference(), Nat8Literal.Create("3"))),
                        VariableDeclaration.CreateStatement("z", null,
                            ExpressionFactory.OptionOf(NameFactory.Nat8NameReference(), Nat8Literal.Create("5"))),

                        VariableDeclaration.CreateStatement("a", NameFactory.Nat8NameReference(), null, env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("b", NameFactory.Nat8NameReference(), null, env.Options.ReassignableModifier()),

                        IfBranch.CreateIf(ExpressionFactory.OptionalAssignment(
                            new[] { NameReference.Create("a"), NameReference.Create("b") },
                            new[] { NameReference.Create("x"), NameReference.Create("z") }),
                            new[] {
                            // assign tracker should recognize the variable is initialized
                        ExpressionFactory.IncBy("acc", NameReference.Create("a")),
                            },
                            // making else branch a dead one
                            IfBranch.CreateElse(ExpressionFactory.GenericThrow())),

                        // assign tracker should recognize the variable is initialized (because `else` branch of above `if` is dead)
                        ExpressionFactory.IncBy("acc", NameReference.Create("b")),

                        Return.Create(NameReference.Create("acc"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)8, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ThrowingException()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "thrower",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.GenericThrow()
                    })));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    FunctionCall.Create(NameReference.Create("thrower")),
                    Return.Create(Int64Literal.Create("1"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.IsTrue(result.IsThrow);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IfBranches()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { Return.Create(Int64Literal.Create("5")) },
                    IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("2"))                }))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

    }
}
