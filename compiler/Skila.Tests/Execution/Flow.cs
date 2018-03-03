using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using System;
using System.Linq;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Flow
    {
        [TestMethod]
        public IInterpreter InitializationWithinOptionalAssignment()
        {
            var env = Language.Environment.Create(new Options()
            {
                DebugThrowOnError = true,
                DiscardingAnyExpressionDuringTests = true,
            });
            var root_ns = env.Root;

            // this test is a bit tougher than regular opt.assignment, because variables will be 
            // initialized for the first time with this assigment
            root_ns.AddBuilder(FunctionBuilder.Create("main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),


                    VariableDeclaration.CreateStatement("x", null,
                        ExpressionFactory.OptionOf(NameFactory.Nat8TypeReference(), Nat8Literal.Create("3"))),
                    VariableDeclaration.CreateStatement("z", null,
                        ExpressionFactory.OptionOf(NameFactory.Nat8TypeReference(), Nat8Literal.Create("5"))),

                    VariableDeclaration.CreateStatement("a", NameFactory.Nat8TypeReference(), null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("b", NameFactory.Nat8TypeReference(), null, EntityModifier.Reassignable),
                  
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

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)8, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ThrowingException()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("thrower"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.GenericThrow()
                })));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    FunctionCall.Create(NameReference.Create("thrower")),
                    Return.Create(Int64Literal.Create("1"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.IsTrue(result.IsThrow);

            return interpreter;
        }


        [TestMethod]
        public IInterpreter IfBranches()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { Return.Create(Int64Literal.Create("5")) },
                    IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("2"))                }))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

    }
}
