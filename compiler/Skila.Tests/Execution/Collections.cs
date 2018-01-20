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
        public IInterpreter MapFunction()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            const string elem_name = "my_elem";

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("pow"),
                NameFactory.IntTypeReference(),
                Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("x", "x"))
                    ))
                .Parameters(FunctionParameter.Create("x", NameFactory.IntTypeReference())));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(
                    // let t *ITuple<Int,Int,Int> = (6,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.TupleTypeReference(NameFactory.IntTypeReference(),
                            NameFactory.IntTypeReference(),
                            // todo: use sink
                            NameFactory.IntTypeReference()),
                        ExpressionFactory.StackConstructor(NameFactory.TupleTypeReference(
                            // todo: use sink for all of them
                            NameFactory.IntTypeReference(), NameFactory.IntTypeReference(), NameFactory.IntTypeReference()),
                        IntLiteral.Create("6"), IntLiteral.Create("-2"))),

                    VariableDeclaration.CreateStatement("m", null,
                        FunctionCall.Create(NameReference.Create("t", NameFactory.MapFunctionName), NameReference.Create("pow"))),

                    VariableDeclaration.CreateStatement("acc", null, IntLiteral.Create("0"), EntityModifier.Reassignable),
                    Loop.CreateForEach(elem_name, NameFactory.IntTypeReference(), NameReference.Create("m"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create(elem_name))) }),
                    Return.Create(NameReference.Create("acc"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(40, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverConcatenatedMixedIterables()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("BasePoint")
                .Modifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(ExpressionFactory.BasicConstructor(new[] { "x" },
                    new[] { NameFactory.IntTypeReference() })));

            root_ns.AddBuilder(TypeBuilder.Create("PointA")
                .Modifier(EntityModifier.Mutable)
                .Parents("BasePoint")
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(),
                    FunctionCall.Constructor(NameReference.CreateBaseInitReference(), NameReference.Create("a")))
                    .Parameters(FunctionParameter.Create("a", NameFactory.IntTypeReference()))));

            root_ns.AddBuilder(TypeBuilder.Create("PointB")
                .Modifier(EntityModifier.Mutable)
                .Parents("BasePoint")
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(),
                    FunctionCall.Constructor(NameReference.CreateBaseInitReference(), NameReference.Create("b")))
                    .Parameters(FunctionParameter.Create("b", NameFactory.IntTypeReference()))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (6,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.TupleTypeReference(NameFactory.PointerTypeReference("PointA"),
                            NameFactory.PointerTypeReference("PointA"),
                            // todo: use sink
                            NameFactory.PointerTypeReference("PointA")),
                        ExpressionFactory.StackConstructor( NameFactory.TupleTypeReference(
                            // todo: use sink for all of them
                            NameFactory.PointerTypeReference("PointA"),
                            NameFactory.PointerTypeReference("PointA"),
                            NameFactory.PointerTypeReference("PointA")),
                        ExpressionFactory.HeapConstructor("PointA",IntLiteral.Create("6")),
                        ExpressionFactory.HeapConstructor("PointA",IntLiteral.Create("-2")))),

                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(NameFactory.PointerTypeReference("PointB")))),

                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("0")),
                        ExpressionFactory.HeapConstructor("PointB",IntLiteral.Create("3"))),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("1")),
                        ExpressionFactory.HeapConstructor("PointB",IntLiteral.Create("-5"))),

                    VariableDeclaration.CreateStatement("c",null,
                        FunctionCall.Create(NameFactory.ConcatReference(),NameReference.Create("t"),NameReference.Create("array"))),

                    VariableDeclaration.CreateStatement("acc",null,IntLiteral.Create("0"),EntityModifier.Reassignable),
                    Loop.CreateForEach("elem",null,NameReference.Create("c"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("elem","x"))) }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverConcatenatedUniformIterables()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            const string elem_name = "my_elem";

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (6,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.TupleTypeReference(NameFactory.IntTypeReference(),
                            NameFactory.IntTypeReference(),
                            // todo: use sink
                            NameFactory.IntTypeReference()),
                        ExpressionFactory.StackConstructor( NameFactory.TupleTypeReference(
                            // todo: use sink for all of them
                            NameFactory.IntTypeReference(), NameFactory.IntTypeReference(),NameFactory.IntTypeReference()),
                        IntLiteral.Create("6"),IntLiteral.Create("-2"))),

                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(NameFactory.IntTypeReference()))),

                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("0")),
                        IntLiteral.Create("3")),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("1")),
                        IntLiteral.Create("-5")),

                    VariableDeclaration.CreateStatement("c",null,
                        FunctionCall.Create(NameFactory.ConcatReference(),NameReference.Create("t"),NameReference.Create("array"))),

                    VariableDeclaration.CreateStatement("acc",null,IntLiteral.Create("0"),EntityModifier.Reassignable),
                    Loop.CreateForEach(elem_name,NameFactory.IntTypeReference(),NameReference.Create("c"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create(elem_name))) }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverAutoResizedArray()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayTypeReference(NameFactory.IntTypeReference()))),

                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("0")),
                        IntLiteral.Create("3")),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("1")),
                        IntLiteral.Create("8")),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),IntLiteral.Create("2")),
                        IntLiteral.Create("-4")),
                    // the same effect as above, but using different route
                    FunctionCall.Create(NameReference.Create("array",NameFactory.AppendFunctionName),IntLiteral.Create("-5")),

                    VariableDeclaration.CreateStatement("acc",null,IntLiteral.Create("0"),EntityModifier.Reassignable),
                    Loop.CreateForEach("elem",NameFactory.IntTypeReference(),NameReference.Create("array"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("elem"))) }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverTuple()
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
                    VariableDeclaration.CreateStatement("acc",null,IntLiteral.Create("0"),EntityModifier.Reassignable),
                    Loop.CreateForEach("elem",NameFactory.IntTypeReference(),NameReference.Create("t"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("elem"))) }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

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
