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
    public class Extensions
    {
        [TestMethod]
        public IInterpreter StaticDispatch()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            Extension ext = root_ns.AddNode(Extension.Create());

            ext.AddBuilder(FunctionBuilder.Create("paf", NameFactory.Nat8TypeReference(), Block.CreateStatement(
                Return.Create(ExpressionFactory.Mul("x", "x"))))
                .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference()),
                    EntityModifier.This)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",null,Nat8Literal.Create("5")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","paf")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)25, result.RetValue.PlainValue);

            return interpreter;
        }


    }
}