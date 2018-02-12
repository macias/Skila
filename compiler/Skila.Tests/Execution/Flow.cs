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

            Assert.AreEqual(DataMode.Throw, result.Mode);

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
