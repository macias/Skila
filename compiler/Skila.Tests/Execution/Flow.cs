using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using System;
using System.Linq;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Flow
    {
        [TestMethod]
        public IInterpreter IfBranches()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { Return.Create(IntLiteral.Create("5")) },
                    IfBranch.CreateElse(new[] { Return.Create(IntLiteral.Create("2"))                }))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

    }
}
