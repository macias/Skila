using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Library
    {
        [TestMethod]
        public IInterpreter DateDayOfWeek()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("d",null,ExpressionFactory.StackConstructor(NameFactory.DateTypeReference(),
                        // it is Friday
                        IntLiteral.Create("2017"),IntLiteral.Create("12"),IntLiteral.Create("29"))),
                    VariableDeclaration.CreateStatement("i",null, 
                        FunctionCall.ConvCall( NameReference.Create("d",NameFactory.DateDayOfWeekProperty),NameFactory.IntTypeReference())),
                    Return.Create(NameReference.Create("i"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(5, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringToInt()
        {
            var env = Environment.Create(new Options() { ThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s",null,StringLiteral.Create("2")),
                    VariableDeclaration.CreateStatement("i",NameFactory.IntTypeReference(),ExpressionFactory.TryGetValue(
                        FunctionCall.Create(NameReference.Create( NameFactory.IntTypeReference(),NameFactory.ParseFunctionName),
                            NameReference.Create("s"))
                        )),
                    Return.Create(NameReference.Create("i"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}