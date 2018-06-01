using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Library
    {
        [TestMethod]
        public IInterpreter RealDividingByZeroWithoutNaNs()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null, Real64Literal.Create(5.0)),
                        VariableDeclaration.CreateStatement("b", null, Real64Literal.Create(0.0)),
                        ExpressionFactory.Readout(ExpressionFactory.Divide("a", "b")),
                        Return.Create(Nat8Literal.Create("2"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.IsTrue(result.IsThrow);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RealNotANumber()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // at this point Skila adheres to the standard but maybe should raise an exception for every NaN
                // and this way remove them from the language (similarly to null pointers)
                // https://stackoverflow.com/questions/5394424/causes-for-nan-in-c-application-that-do-no-raise-a-floating-point-exception
                // https://stackoverflow.com/questions/2941611/can-i-make-gcc-tell-me-when-a-calculation-results-in-nan-or-inf-at-runtime
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowRealMagic = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null, Real64Literal.Create(double.NaN)),
                        VariableDeclaration.CreateStatement("b", null, Real64Literal.Create(double.NaN)),
                        Return.Create(ExpressionFactory.Ternary(ExpressionFactory.IsEqual("a", "b"),
                            Nat8Literal.Create("15"), Nat8Literal.Create("2")))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)2, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DateDayOfWeek()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.NatTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("d",null,ExpressionFactory.StackConstructor(NameFactory.DateTypeReference(),
                        // it is Friday
                        Int16Literal.Create("2017"),Nat8Literal.Create("12"),Nat8Literal.Create("29"))),
                    VariableDeclaration.CreateStatement("i",null,
                        FunctionCall.ConvCall( NameReference.Create("d",NameFactory.DateDayOfWeekProperty),NameFactory.NatTypeReference())),
                    Return.Create(NameReference.Create("i"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(5UL, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringToInt()
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
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s",null,StringLiteral.Create("2")),
                    VariableDeclaration.CreateStatement("i",NameFactory.Int64TypeReference(),ExpressionFactory.GetOptionValue(
                        FunctionCall.Create(NameReference.Create( NameFactory.Int64TypeReference(),NameFactory.ParseFunctionName),
                            NameReference.Create("s"))
                        )),
                    Return.Create(NameReference.Create("i"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}