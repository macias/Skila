using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Entities;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class FunctionCalls
    {
        [TestMethod]
        public IInterpreter StoringSequenceAsSequence()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // todo: add analogous test with storing iterable as sequence

                // yes, this test seems like pointless thing but it is important nevertheless because
                // we have to check if conversion does NOT happen
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    ExpressionFactory.InitializeIndexable("x",Int64Literal.Create("-6"),Int64Literal.Create("8")),
                    // conversion should NOT happen
                    VariableDeclaration.CreateStatement("stored",null,
                        FunctionCall.Create( NameFactory.StoreFunctionReference(),NameReference.Create("x"))),
                    Return.Create(ExpressionFactory.Ternary(IsSame.Create("x","stored"),Nat8Literal.Create("2"),Nat8Literal.Create("7")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)2, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OptionalNoLimitsVariadicFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("provider", NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", null,
                            ExpressionFactory.StackConstructor(NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                                FunctionArgument.Create(NatLiteral.Create("2")))),
                        Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("x"), FunctionArgument.Create(NatLiteral.Create("0"))),
                            Int64Literal.Create("-6")),
                        Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("x"), FunctionArgument.Create(NatLiteral.Create("1"))),
                            Int64Literal.Create("8")),
                        Return.Create(NameReference.Create("x"))
                        )));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(),
                        FunctionCall.Create(NameReference.Create("provider")), isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("sum")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter MinMaxLimitVariadicFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(2, 11), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        Int64Literal.Create("-6"),Int64Literal.Create("8")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter MinLimitVariadicFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(2), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        Int64Literal.Create("-6"),Int64Literal.Create("8")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter NoLimitsVariadicFunction()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        Int64Literal.Create("-6"),Int64Literal.Create("8")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter MinMaxLimitVariadicFunctionWithSpread()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(2, 11), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("8")),
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        FunctionArgument.Create( Spread.Create(NameReference.Create("x")))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter MinLimitVariadicFunctionWithSpread()
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
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(2), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("8")),
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        FunctionArgument.Create( Spread.Create(NameReference.Create("x")))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter NoLimitsVariadicFunctionWithSpread()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sum",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(NatLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.Create(), null, isNameRequired: false)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.Int64TypeReference()),
                            FunctionArgument.Create(NatLiteral.Create("2"))),
                        env.Options.ReassignableModifier()),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("0"))),
                        Int64Literal.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(NatLiteral.Create("1"))),
                        Int64Literal.Create("8")),
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        FunctionArgument.Create( Spread.Create(NameReference.Create("x")))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter LambdaRecursiveCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression i_eq_2 = ExpressionFactory.IsEqual(NameReference.Create("i"), Int64Literal.Create("2"));
                IExpression i_add_1 = ExpressionFactory.Add(NameReference.Create("i"), Int64Literal.Create("1"));
                FunctionDefinition lambda = FunctionBuilder.CreateLambda(NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                    // if i==2 then return i
                    IfBranch.CreateIf(i_eq_2,new[]{ Return.Create(NameReference.Create("i")) },
                    // else return self(i+1)
                    IfBranch.CreateElse(new[]{
                        Return.Create(FunctionCall.Create(NameReference.Create(NameFactory.RecurFunctionName),
                            FunctionArgument.Create(i_add_1)))
                    }))
                    }))
                    .Parameters(FunctionParameter.Create("i", NameFactory.Int64TypeReference()));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // Immediately-Invoked Function Expression (IIEFE) in Javascript world
                    Return.Create(FunctionCall.Create(lambda,FunctionArgument.Create(Int64Literal.Create("0"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RecursiveCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression i_eq_2 = ExpressionFactory.IsEqual(NameReference.Create("i"), Int64Literal.Create("2"));
                IExpression i_add_1 = ExpressionFactory.Add(NameReference.Create("i"), Int64Literal.Create("1"));
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                    // if i==2 then return i
                    IfBranch.CreateIf(i_eq_2,new[]{ Return.Create(NameReference.Create("i")) },
                    // else return self(i+1)
                    IfBranch.CreateElse(new[]{
                        Return.Create(FunctionCall.Create(NameReference.Create(NameFactory.RecurFunctionName),
                            FunctionArgument.Create(i_add_1)))
                    }))
                    }))
                    .Parameters(FunctionParameter.Create("i", NameFactory.Int64TypeReference())));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create(Int64Literal.Create("0"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RawMethods()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {

                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create( Int64Literal.Create("1"),NameFactory.AddOperator),
                    FunctionArgument.Create(Int64Literal.Create("1"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ReturningUnit()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "getMe",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create()
                    })));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("u",NameFactory.UnitTypeReference(),
                        FunctionCall.Create(NameReference.Create("getMe"))),
                    ExpressionFactory.Readout("u"),
                    Return.Create(Int64Literal.Create("2"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OptionalParameters()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(),
                        Int64Literal.Create("2"), env.Options.ReassignableModifier()))
                    .With(FunctionBuilder.Create("pass",
                        ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                        Return.Create(NameReference.Create("v"))
                        }))
                        .Parameters(FunctionParameter.Create("v", NameFactory.Int64TypeReference(), Variadic.None,
                                FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName, "getme")), isNameRequired: false)))
                    .With(FunctionBuilder.Create("getme", null,
                        ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                        Return.Create(NameReference.Create(NameFactory.ThisVariableName, "x"))
                        }))));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("p","pass")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter UsingFunctionParameter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "inc",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create( NameReference.Create("n"),NameFactory.AddOperator),
                    FunctionArgument.Create(Int64Literal.Create("1"))))
                    }))
                    .Parameters(FunctionParameter.Create("n", NameFactory.Int64TypeReference(), Variadic.None, null, isNameRequired: false)));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                     Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( Int64Literal.Create("1"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter LocalVariablesLeakCheck()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "last",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("z",null,Int64Literal.Create("2")),
                    Return.Create( NameReference.Create( "z"))
                    })));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "pass",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("z",null,Int64Literal.Create("0")),
                    VariableDeclaration.CreateStatement("t",null,FunctionCall.Create( NameReference.Create("last"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("z"),NameReference.Create("t")))
                    })));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                     Return.Create( FunctionCall.Create(NameReference.Create("pass")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
