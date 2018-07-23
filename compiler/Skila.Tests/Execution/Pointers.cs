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
    public class Pointers
    {
        [TestMethod]
        public IInterpreter TestingSamePointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    AllowInvalidMainResult = true,
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true
                }.SetMutability(mutability));
                var root_ns = env.Root;


                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),Int64Literal.Create("2"))),
                    VariableDeclaration.CreateStatement("y",null,NameReference.Create("x")),
                    VariableDeclaration.CreateStatement("z",null,
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),Int64Literal.Create("2"))),
                    VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), env.Options.ReassignableModifier()),
                    IfBranch.CreateIf(IsSame.Create(NameReference.Create("x"),NameReference.Create("y")),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),Int64Literal.Create("2")))
                    }),
                    IfBranch.CreateIf(IsSame.Create(NameReference.Create("x"),NameReference.Create("z")),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),Int64Literal.Create("7")))
                    }),
                    Return.Create(NameReference.Create("acc"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RefCountsOnReadingFunctionCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // the aim of this test is to test heap manager if it correctly handles reading function which returns pointer

                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "provider",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",null,
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),
                            FunctionArgument.Create(Int64Literal.Create("77")))),
                    Return.Create( NameReference.Create("p_int")),
                    })));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.Int64NameReference(),
                        FunctionCall.Create(NameReference.Create("provider"))),
                    Return.Create(NameReference.Create("i")),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(77L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RefCountsOnIgnoringFunctionCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // the aim of this test is to test heap manager if it correctly handles ignoring function which returns pointer

                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    AllowInvalidMainResult = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "provider",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),FunctionArgument.Create(Int64Literal.Create("77")))),
                    Return.Create( NameReference.Create("p_int")),
                    })));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    FunctionCall.Create(NameReference.Create("provider")),
                    Return.Create( Int64Literal.Create("2")),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StackChunkWithBasicPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    AllowInvalidMainResult = true,
                    DebugThrowOnError = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("chicken",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkNameReference(NameFactory.PointerNameReference( NameFactory.Int64NameReference())),
                            FunctionArgument.Create(NatLiteral.Create("2")))),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("chicken"),NatLiteral.Create("0")),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(), Int64Literal.Create("-6"))),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("chicken"),NatLiteral.Create("1")),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(), Int64Literal.Create("8"))),
                    Return.Create(ExpressionFactory.Add(FunctionCall.Indexer(NameReference.Create("chicken"),NatLiteral.Create("0")),
                        FunctionCall.Indexer(NameReference.Create("chicken"),NatLiteral.Create("1"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter PointerArgumentAutoDereference()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    AllowInvalidMainResult = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "inc",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(ExpressionFactory.Add(NameReference.Create("n"),Int64Literal.Create("1")))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64NameReference())));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),FunctionArgument.Create(Int64Literal.Create("1")))),
                    Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( NameReference.Create("p_int")))),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DereferenceOnReturn()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    AllowInvalidMainResult = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),FunctionArgument.Create(Int64Literal.Create("2")))),
                    Return.Create( NameReference.Create("p_int")),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DereferenceOnIfCondition()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    AllowInvalidMainResult = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ptr",NameFactory.PointerNameReference(NameFactory.BoolNameReference()),
                        ExpressionFactory.HeapConstructor(NameFactory.BoolNameReference(),FunctionArgument.Create(BoolLiteral.CreateTrue()))),
                    Return.Create( IfBranch.CreateIf( NameReference.Create("ptr"),new[]{ Int64Literal.Create("2") },
                        IfBranch.CreateElse(new[]{ Int64Literal.Create("5") }))),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ExplicitDereferencing()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    AllowInvalidMainResult = true,
                    DebugThrowOnError = true,
                    AllowDereference = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl = VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerNameReference(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability())),
                            ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability()),
                                FunctionArgument.Create(Int64Literal.Create("4"))), 
                            env.Options.ReassignableModifier());
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // x *Int = new Int(4)
                    decl,
                    // y *Int = x
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerNameReference(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability())),
                        NameReference.Create("x")),
                    // *x = 7 // y <- 7
                    Assignment.CreateStatement(Dereference.Create( NameReference.Create("x")),Int64Literal.Create("7")),
                    // z *Int
                    VariableDeclaration.CreateStatement("z",
                        NameFactory.PointerNameReference(NameFactory.Int64NameReference( env.Options.ReassignableTypeMutability())),
                        null, env.Options.ReassignableModifier()),
                    // z = x
                    Assignment.CreateStatement(NameReference.Create("z"), NameReference.Create("x")),
                    // v = y + z  // 14
                    VariableDeclaration.CreateStatement("v", null, ExpressionFactory.Add("y", "z"), env.Options.ReassignableModifier()),
                    // x = -12
                    Assignment.CreateStatement(NameReference.Create("x"),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability()),
                            FunctionArgument.Create(Int64Literal.Create("-12")))),
                    // *z = *x   // z <- -12
                    Assignment.CreateStatement(Dereference.Create(NameReference.Create("z")), Dereference.Create(NameReference.Create("x"))),
                    // x = -1000
                    Assignment.CreateStatement(NameReference.Create("x"),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability()),
                            FunctionArgument.Create(Int64Literal.Create("-1000")))),
                    // v = v+z  // v = 14 + (-12)
                    Assignment.CreateStatement(NameReference.Create("v"), ExpressionFactory.Add("v", "z")),
                    Return.Create(NameReference.Create("v"))
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DereferenceOnAssignment()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    AllowInvalidMainResult = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // p_int *Int = new Int(1)
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerNameReference(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability() )),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability()),
                            FunctionArgument.Create(Int64Literal.Create("1"))),env.Options.ReassignableModifier()),
                    // v_int Int = *p_int // automatic dereference
                    VariableDeclaration.CreateStatement("v_int",NameFactory.Int64NameReference(), NameReference.Create("p_int")),
                    // z_int Int
                    VariableDeclaration.CreateStatement("z_int",NameFactory.Int64NameReference(), null,env.Options.ReassignableModifier()),
                    // z_int = *p_int // automatic derereference
                    Assignment.CreateStatement(NameReference.Create( "z_int"), NameReference.Create("p_int")),
                    // p_int = new Int(77)
                    Assignment.CreateStatement(NameReference.Create("p_int"),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(env.Options.ReassignableTypeMutability()),
                            FunctionArgument.Create(Int64Literal.Create("77")))),
                    // return v_int + z_int 
                    Return.Create( FunctionCall.Create(NameReference.Create( NameReference.Create("v_int"),NameFactory.AddOperator),
                        FunctionArgument.Create(NameReference.Create("z_int"))))
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DirectRefCountings()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "inc",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let temp *Int = n; // n is argument
                    VariableDeclaration.CreateStatement("temp",NameFactory.PointerNameReference( NameFactory.Int64NameReference()),
                        NameReference.Create("n")),
                    // return temp + 1;
                    Return.Create(FunctionCall.Create(NameReference.Create( NameReference.Create("temp"),NameFactory.AddOperator),
                        FunctionArgument.Create(Int64Literal.Create("1"))))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.PointerNameReference(NameFactory.Int64NameReference()))));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let p_int *Int = new *1;
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                        ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(),FunctionArgument.Create(Int64Literal.Create("1")))),
                    // let proxy *Int = p_int;
                    VariableDeclaration.CreateStatement("proxy",NameFactory.PointerNameReference(NameFactory.Int64NameReference()),NameReference.Create("p_int")),
                    // return inc(proxy);
                    Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( NameReference.Create("proxy")))),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter NestedRefCountings()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                var chain_type = root_ns.AddBuilder(TypeBuilder.Create("Chain")
                    .SetModifier(EntityModifier.Mutable)
                    .With(VariableDeclaration.CreateStatement("v", NameFactory.Int64NameReference(), null,
                        EntityModifier.Public | env.Options.ReassignableModifier()))
                    .With(VariableDeclaration.CreateStatement("n", NameFactory.PointerNameReference(NameReference.Create("Chain")),
                        Undef.Create(), EntityModifier.Public | env.Options.ReassignableModifier())));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerNameReference(NameReference.Create("Chain")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Chain"))),
                    Assignment.CreateStatement(NameReference.Create("p","n"),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Chain"))),
                    Assignment.CreateStatement(NameReference.Create("p","n","v"),Int64Literal.Create("2")),
                    Return.Create(NameReference.Create("p","n","v")),
                    })));


                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
