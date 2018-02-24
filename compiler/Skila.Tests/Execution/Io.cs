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
    public class Io
    {
        private const string randomTextFilePath = "Data/random_text.utf8.txt";

        [TestMethod]
        public IInterpreter FileExists()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("e", null, FunctionCall.Create(NameReference.Create(NameFactory.FileTypeReference(),
                        NameFactory.FileExists), StringLiteral.Create(randomTextFilePath))),
                    IfBranch.CreateIf(NameReference.Create("e"), new[] { Return.Create(Int64Literal.Create("2")) },
                        IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("-5")) }))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter FileReadingLines()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat64TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("lines", null,
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(NameFactory.FileTypeReference(), NameFactory.FileReadLines),
                            StringLiteral.Create(randomTextFilePath)))),
                    // first line is "It was" (without quotes)
                    VariableDeclaration.CreateStatement("first", null,
                        FunctionCall.Create(NameReference.Create("lines", NameFactory.PropertyIndexerName), NatLiteral.Create("0"))),
                    // 6
                    VariableDeclaration.CreateStatement("len", null,
                        NameReference.Create("first", NameFactory.IterableCount)),
                    Return.Create(NameReference.Create("len"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(6UL, result.RetValue.PlainValue);

            return interpreter;
        }

    }
}