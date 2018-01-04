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
        public IInterpreter AccessingTuple()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (4,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.PointerTypeReference( NameFactory.ITupleMutableTypeReference(NameFactory.IntTypeReference(),
                            NameFactory.IntTypeReference(),
                            // todo: use sink
                            NameFactory.IntTypeReference())),
                        ExpressionFactory.HeapConstructor( NameFactory.TupleTypeReference(
                            // todo: use sink for all of them
                            NameFactory.IntTypeReference(), NameFactory.IntTypeReference(),NameFactory.IntTypeReference()),
                        IntLiteral.Create("4"),IntLiteral.Create("-2"))),
                    // let v0 = t.item0
                    VariableDeclaration.CreateStatement("v0",null,NameReference.Create("t",NameFactory.TupleItemName(0))),
                    // let v1 = t[1]
                    VariableDeclaration.CreateStatement("v1",null,FunctionCall.Indexer(NameReference.Create("t"),IntLiteral.Create("1"))),
                    // return v0+v1
                    Return.Create(ExpressionFactory.Add(NameReference.Create("v0"),NameReference.Create("v1")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

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
