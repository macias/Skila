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
    public class Closures
    {
//        [TestMethod]
        public IInterpreter TODO_ClosureRecursiveCall()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            IExpression i_eq_jack = ExpressionFactory.IsEqual(NameReference.Create("i"), NameReference.Create("jack"));
            IExpression i_add_1 = ExpressionFactory.Add(NameReference.Create("i"), Nat8Literal.Create("1"));
            FunctionDefinition lambda = FunctionBuilder.CreateLambda(NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new[] {
                    // if i==jack then return i
                    IfBranch.CreateIf(i_eq_jack,new[]{ Return.Create(NameReference.Create("i")) },
                    // else return self(i+1)
                    IfBranch.CreateElse(new[]{
                        Return.Create(FunctionCall.Create(NameReference.Create(NameFactory.SelfFunctionName),
                            FunctionArgument.Create(i_add_1)))
                    }))
                }))
                .Parameters(FunctionParameter.Create("i", NameFactory.Nat8TypeReference()));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("jack",null,Nat8Literal.Create("50")),
                    // Immediately-Invoked Function Expression (IIEFE) in Javascript world
                    Return.Create(FunctionCall.Create(lambda,FunctionArgument.Create(Nat8Literal.Create("0"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)50, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter EmptyClosure()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("getIt"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                        Return.Create(Int64Literal.Create("2"))
                    })));

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // f = getIt 
                    VariableDeclaration.CreateStatement("f",null,NameReference.Create("getIt")),
                    // return f()
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ImplicitClosureWithValue()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Beep")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("m", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(FunctionBuilder.Create(NameDefinition.Create("getIt"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                        Return.Create(NameReference.Create(NameFactory.ThisVariableName, "m"))
                    }))));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // b = new Beep()
                    VariableDeclaration.CreateStatement("b",null,ExpressionFactory.StackConstructor(NameReference.Create("Beep"))),
                    // b.m = 2
                    Assignment.CreateStatement(NameReference.Create("b","m"),Int64Literal.Create("2")),
                    // f = b.getIt // "b" value is sucked in, so we have a copy
                    VariableDeclaration.CreateStatement("f",null,NameReference.Create("b","getIt")),
                    // b.m = 5
                    Assignment.CreateStatement(NameReference.Create("b","m"),Int64Literal.Create("5")),
                    // return f()
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ImplicitClosureWithPointer()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Beep")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("m", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(FunctionBuilder.Create(NameDefinition.Create("getIt"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                        Return.Create(NameReference.Create(NameFactory.ThisVariableName, "m"))
                    }))));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("b",null,ExpressionFactory.HeapConstructor(NameReference.Create("Beep"))),
                    Assignment.CreateStatement(NameReference.Create("b","m"),Int64Literal.Create("5")),
                    // pointer of "b" value is sucked in, so we will see any changes 
                    VariableDeclaration.CreateStatement("f",null,NameReference.Create("b","getIt")),
                    Assignment.CreateStatement(NameReference.Create("b","m"),Int64Literal.Create("2")),
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter PassingLocalVariables()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true });
            var root_ns = env.Root;

            NameReference escaping_lambda = NameReference.Create("x");
            IExpression lambda = FunctionBuilder.CreateLambda(NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] { Return.Create(escaping_lambda) })).Build();
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // x = 2
                    VariableDeclaration.CreateStatement("x",null,Int64Literal.Create("2"),EntityModifier.Reassignable),
                    // f = () => x
                    VariableDeclaration.CreateStatement("f",null,lambda),
                    // x = 3
                    Assignment.CreateStatement(NameReference.Create("x"),Int64Literal.Create("3")),
                    // return f()
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ResultTypeInference()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            IExpression lambda = FunctionBuilder.CreateLambda(null,
                    Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("2")) })).Build();
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // f = () => x
                    VariableDeclaration.CreateStatement("f",null,lambda),
                    // return f()
                    Return.Create(FunctionCall.Create(NameReference.Create("f")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

    }
}