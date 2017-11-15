using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Closures
    {
       // [TestMethod]
        public IInterpreter TODO_PassingLocalVariables()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition lambda = FunctionDefinition.CreateLambda(NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] { Return.Create(NameReference.Create("x")) }));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,IntLiteral.Create("2"),EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("f",null,lambda),
                    Assignment.CreateStatement(NameReference.Create("x"),IntLiteral.Create("3")),
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}