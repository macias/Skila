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
    public class Collections
    {
        [TestMethod]
        public IInterpreter AllFailureFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "below",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.IsLess(NameReference.Create("x"), Int64Literal.Create("5")))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("6"), Int64Literal.Create("2"))),

                        Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("t", NameFactory.AllFunctionName),
                            NameReference.Create("below")), Nat8Literal.Create("9"), Nat8Literal.Create("0")))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AllSuccessFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "below",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.IsLess(NameReference.Create("x"), Int64Literal.Create("5")))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("4"), Int64Literal.Create("2"))),

                        Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("t", NameFactory.AllFunctionName),
                            NameReference.Create("below")), Nat8Literal.Create("9"), Nat8Literal.Create("0")))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)9, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AnyFailureFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "below",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.IsLess(NameReference.Create("x"), Int64Literal.Create("5")))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("6"), Int64Literal.Create("7"))),

                        Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("t", NameFactory.AnyFunctionName),
                            NameReference.Create("below")), Nat8Literal.Create("9"), Nat8Literal.Create("0")))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AnySuccessFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "below",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.IsLess(NameReference.Create("x"), Int64Literal.Create("5")))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("6"), Int64Literal.Create("2"))),

                        Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("t", NameFactory.AnyFunctionName),
                            NameReference.Create("below")), Nat8Literal.Create("9"), Nat8Literal.Create("0")))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)9, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ReverseFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        // let t ITuple<Nat8,Nat8,Nat8> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Nat8NameReference(),
                                NameFactory.Nat8NameReference(),
                                // todo: use sink
                                NameFactory.Nat8NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Nat8NameReference(), NameFactory.Nat8NameReference(), NameFactory.Nat8NameReference()),
                            Nat8Literal.Create("7"), Nat8Literal.Create("11"))),

                        VariableDeclaration.CreateStatement("w", null, Nat8Literal.Create("3"), env.Options.ReassignableModifier()),

                        VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), env.Options.ReassignableModifier()),
                        Loop.CreateForEach("el", NameFactory.Nat8NameReference(),
                            FunctionCall.Create(NameReference.Create("t", NameFactory.ReverseFunctionName)),
                            new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),
                                ExpressionFactory.Mul("w","el"))),

                            ExpressionFactory.IncBy("w",Nat8Literal.Create("2"))
                                }),
                        Return.Create(NameReference.Create("acc"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)(11 * 3 + 7 * 5), result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter FilterFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                const string elem_name = "my_elem";

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "below",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.IsLess(NameReference.Create("x"), Int64Literal.Create("5")))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("6"), Int64Literal.Create("2"))),

                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("t", NameFactory.FilterFunctionName), NameReference.Create("below"))),

                        VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), env.Options.ReassignableModifier()),
                        Loop.CreateForEach(elem_name, NameFactory.Int64NameReference(), NameReference.Create("m"),
                            new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create(elem_name))) }),
                        Return.Create(NameReference.Create("acc"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter MapFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                const string elem_name = "my_elem";

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "pow",
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        Return.Create(ExpressionFactory.Mul("x", "x"))
                        ))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference())));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        // let t *ITuple<Int,Int,Int> = (6,-2)
                        VariableDeclaration.CreateStatement("t",
                            NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                                NameFactory.Int64NameReference(),
                                // todo: use sink
                                NameFactory.Int64NameReference()),
                            ExpressionFactory.StackConstructor(NameFactory.TupleNameReference(
                                // todo: use sink for all of them
                                NameFactory.Int64NameReference(), NameFactory.Int64NameReference(), NameFactory.Int64NameReference()),
                            Int64Literal.Create("6"), Int64Literal.Create("-2"))),

                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("t", NameFactory.MapFunctionName), NameReference.Create("pow"))),

                        VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), env.Options.ReassignableModifier()),
                        Loop.CreateForEach(elem_name, NameFactory.Int64NameReference(), NameReference.Create("m"),
                            new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create(elem_name))) }),
                        Return.Create(NameReference.Create("acc"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(40L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverConcatenatedMixedIterables()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("BasePoint")
                    .SetModifier(EntityModifier.Mutable | EntityModifier.Base)
                    .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), null,
                        EntityModifier.Public | env.Options.ReassignableModifier()))
                    .With(ExpressionFactory.BasicConstructor(new[] { "x" },
                        new[] { NameFactory.Int64NameReference() })));

                root_ns.AddBuilder(TypeBuilder.Create("PointA")
                    .SetModifier(EntityModifier.Mutable)
                    .Parents("BasePoint")
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(),
                        FunctionCall.Constructor(NameReference.CreateBaseInitReference(), NameReference.Create("a")))
                        .Parameters(FunctionParameter.Create("a", NameFactory.Int64NameReference()))));

                root_ns.AddBuilder(TypeBuilder.Create("PointB")
                    .SetModifier(EntityModifier.Mutable)
                    .Parents("BasePoint")
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(),
                        FunctionCall.Constructor(NameReference.CreateBaseInitReference(), NameReference.Create("b")))
                        .Parameters(FunctionParameter.Create("b", NameFactory.Int64NameReference()))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let t ITuple<*PointA,*PointA,*PointA> = (6,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.TupleNameReference(
                            NameFactory.PointerNameReference("PointA"),
                            NameFactory.PointerNameReference("PointA"),
                            // todo: use wildcard
                            NameFactory.PointerNameReference("PointA")),
                        ExpressionFactory.StackConstructor( NameFactory.TupleNameReference(
                            // todo: use wildcard for all of them
                            NameFactory.PointerNameReference("PointA"),
                            NameFactory.PointerNameReference("PointA"),
                            NameFactory.PointerNameReference("PointA")),
                        ExpressionFactory.HeapConstructor("PointA",Int64Literal.Create("6")),
                        ExpressionFactory.HeapConstructor("PointA",Int64Literal.Create("-2")))),

                    // var array = new *PointB[3,-5];
                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(NameFactory.PointerNameReference("PointB")))),
                    ExpressionFactory.InitializeIndexable("array",
                        ExpressionFactory.HeapConstructor("PointB",Int64Literal.Create("3")),
                        ExpressionFactory.HeapConstructor("PointB",Int64Literal.Create("-5"))),

                    // let c = concat(t,array) 
                    // in effect we should get iterable of `PointBase` 
                    VariableDeclaration.CreateStatement("c",null,
                        FunctionCall.Create(NameFactory.ConcatReference(),NameReference.Create("t"),NameReference.Create("array"))),

                    VariableDeclaration.CreateStatement("acc",null,Int64Literal.Create("0"),env.Options.ReassignableModifier()),
                    Loop.CreateForEach("pt",
                        null,
                        NameReference.Create("c"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("pt","x"))) }),
                    Return.Create(NameReference.Create("acc"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverConcatenatedUniformIterables()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                const string elem_name = "my_elem";

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (6,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.TupleNameReference(NameFactory.Int64NameReference(),
                            NameFactory.Int64NameReference(),
                            // todo: use sink
                            NameFactory.Int64NameReference()),
                        ExpressionFactory.StackConstructor( NameFactory.TupleNameReference(
                            // todo: use sink for all of them
                            NameFactory.Int64NameReference(), NameFactory.Int64NameReference(),NameFactory.Int64NameReference()),
                        Int64Literal.Create("6"),Int64Literal.Create("-2"))),

                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(NameFactory.Int64NameReference()))),

                    ExpressionFactory.InitializeIndexable("array",Int64Literal.Create("3"),Int64Literal.Create("-5")),

                    VariableDeclaration.CreateStatement("c",null,
                        FunctionCall.Create(NameFactory.ConcatReference(),NameReference.Create("t"),NameReference.Create("array"))),

                    VariableDeclaration.CreateStatement("acc",null,Int64Literal.Create("0"),env.Options.ReassignableModifier()),
                    Loop.CreateForEach(elem_name,NameFactory.Int64NameReference(),NameReference.Create("c"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create(elem_name))) }),
                    Return.Create(NameReference.Create("acc"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverAutoResizedArray()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("array",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ArrayNameReference(NameFactory.Int64NameReference()))),

                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),NatLiteral.Create("0")),
                        Int64Literal.Create("3")),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),NatLiteral.Create("1")),
                        Int64Literal.Create("8")),
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("array"),NatLiteral.Create("2")),
                        Int64Literal.Create("-4")),
                    // the same effect as above, but using different route
                    FunctionCall.Create(NameReference.Create("array",NameFactory.AppendFunctionName),Int64Literal.Create("-5")),

                    VariableDeclaration.CreateStatement("acc",null,Int64Literal.Create("0"),env.Options.ReassignableModifier()),
                    Loop.CreateForEach("elem",NameFactory.Int64NameReference(),NameReference.Create("array"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("elem"))) }),
                    Return.Create(NameReference.Create("acc"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter IteratingOverTuple()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (4,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.PointerNameReference( NameFactory.ITupleMutableNameReference(NameFactory.Int64NameReference(),
                            NameFactory.Int64NameReference(),
                            // todo: use sink
                            NameFactory.Int64NameReference())),
                        ExpressionFactory.HeapConstructor( NameFactory.TupleNameReference(
                            // todo: use sink for all of them
                            NameFactory.Int64NameReference(), NameFactory.Int64NameReference(),NameFactory.Int64NameReference()),
                        Int64Literal.Create("4"),Int64Literal.Create("-2"))),
                    VariableDeclaration.CreateStatement("acc",null,Int64Literal.Create("0"),env.Options.ReassignableModifier()),
                    Loop.CreateForEach("elem",NameFactory.Int64NameReference(),NameReference.Create("t"),
                        new[]{ Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),NameReference.Create("elem"))) }),
                    Return.Create(NameReference.Create("acc"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AccessingTuple()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let t *ITuple<Int,Int,Int> = (4,-2)
                    VariableDeclaration.CreateStatement("t",
                        NameFactory.PointerNameReference( NameFactory.ITupleMutableNameReference(NameFactory.Int64NameReference(),
                            NameFactory.Int64NameReference(),
                            // todo: use sink
                            NameFactory.Int64NameReference())),
                        ExpressionFactory.HeapConstructor( NameFactory.TupleNameReference(
                            // todo: use sink for all of them
                            NameFactory.Int64NameReference(), NameFactory.Int64NameReference(),NameFactory.Int64NameReference()),
                        Int64Literal.Create("4"),Int64Literal.Create("-2"))),
                    // let v0 = t.item0
                    VariableDeclaration.CreateStatement("v0",null,NameReference.Create("t",NameFactory.TupleItemName(0))),
                    // let v1 = t[1]
                    VariableDeclaration.CreateStatement("v1",null,FunctionCall.Indexer(NameReference.Create("t"),NatLiteral.Create("1"))),
                    // return v0+v1
                    Return.Create(ExpressionFactory.Add(NameReference.Create("v0"),NameReference.Create("v1")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ChunkOnStack()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    VariableDeclaration.CreateStatement("y",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("8")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("33")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("33")),
                    Assignment.CreateStatement(NameReference.Create("y"),NameReference.Create("x")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("777")),
                    Return.Create(ExpressionFactory.Add(FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("1")))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ChunkOnHeap()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    VariableDeclaration.CreateStatement("y",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkNameReference(NameFactory.Int64NameReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),

                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("20")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("33")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("33")),

                    Assignment.CreateStatement(NameReference.Create("y"),NameReference.Create("x")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-18")),
                    Return.Create(ExpressionFactory.Add(FunctionCall.Indexer(NameReference.Create("y"),
                        FunctionArgument.Create(NatLiteral.Create("0"))),
                        FunctionCall.Indexer(NameReference.Create("y"),FunctionArgument.Create(NatLiteral.Create("1")))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
