using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Collections
    {
        [TestMethod]
        public IInterpreter ChunkOnStack()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("1"))),
                        IntLiteral.Create("8")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("33")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("1"))),
                        IntLiteral.Create("33")),
                    Assignment.CreateStatement(NameReference.Create("y"),NameReference.Create("x")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("777")),
                    Return.Create(ExpressionFactory.Add(FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("1")))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ChunkOnHeap()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("1"))),
                        IntLiteral.Create("20")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("33")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("1"))),
                        IntLiteral.Create("33")),
                    Assignment.CreateStatement(NameReference.Create("y"),NameReference.Create("x")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("-18")),
                    Return.Create(ExpressionFactory.Add(FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(IntLiteral.Create("1")))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
