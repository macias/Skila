﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Entities;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class FunctionCalls
    {
        [TestMethod]
        public IInterpreter NoLimitsVariadicFunctionWithSpread()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("sum"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // let i0 = n.at(0)
                    VariableDeclaration.CreateStatement("i0",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(IntLiteral.Create("0")))),
                    // let i1 = n.at(1)
                    VariableDeclaration.CreateStatement("i1",null,FunctionCall.Create(NameReference.Create("n",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(IntLiteral.Create("1")))),
                    // return i0+i1
                    Return.Create(ExpressionFactory.Add("i0","i1"))
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.IntTypeReference(), Variadic.Create(), null, isNameRequired: false)));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChunkTypeReference(NameFactory.IntTypeReference()),
                            FunctionArgument.Create(IntLiteral.Create("2"))),
                        EntityModifier.Reassignable),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("0"))),
                        IntLiteral.Create("-6")),
                    Assignment.CreateStatement( FunctionCall.Indexer(NameReference.Create("x"),FunctionArgument.Create(IntLiteral.Create("1"))),
                        IntLiteral.Create("8")),
                    Return.Create(FunctionCall.Create(NameReference.Create("sum"),
                        FunctionArgument.Create( Spread.Create(NameReference.Create("x")))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }


        [TestMethod]
        public IInterpreter LambdaRecursiveCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            IExpression i_eq_2 = ExpressionFactory.Equal(NameReference.Create("i"), IntLiteral.Create("2"));
            IExpression i_add_1 = ExpressionFactory.Add(NameReference.Create("i"), IntLiteral.Create("1"));
            FunctionDefinition lambda = FunctionBuilder.CreateLambda(NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] {
                    // if i==2 then return i
                    IfBranch.CreateIf(i_eq_2,new[]{ Return.Create(NameReference.Create("i")) },
                    // else return self(i+1)
                    IfBranch.CreateElse(new[]{
                        Return.Create(FunctionCall.Create(NameReference.Create(NameFactory.SelfFunctionName),
                            FunctionArgument.Create(i_add_1)))
                    }))
                }))
                .Parameters(FunctionParameter.Create("i", NameFactory.IntTypeReference()));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // Immediately-Invoked Function Expression (IIEFE) in Javascript world
                    Return.Create(FunctionCall.Create(lambda,FunctionArgument.Create(IntLiteral.Create("0"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RecursiveCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            IExpression i_eq_2 = ExpressionFactory.Equal(NameReference.Create("i"), IntLiteral.Create("2"));
            IExpression i_add_1 = ExpressionFactory.Add(NameReference.Create("i"), IntLiteral.Create("1"));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] {
                    // if i==2 then return i
                    IfBranch.CreateIf(i_eq_2,new[]{ Return.Create(NameReference.Create("i")) },
                    // else return self(i+1)
                    IfBranch.CreateElse(new[]{
                        Return.Create(FunctionCall.Create(NameReference.Create(NameFactory.SelfFunctionName),
                            FunctionArgument.Create(i_add_1)))
                    }))
                }))
                .Parameters(FunctionParameter.Create("i", NameFactory.IntTypeReference())));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create(IntLiteral.Create("0"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RawMethods()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create( IntLiteral.Create("1"),NameFactory.AddOperator),
                    FunctionArgument.Create(IntLiteral.Create("1"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ReturningUnit()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getMe"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create()
                })));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("u",NameFactory.UnitTypeReference(),
                        FunctionCall.Create(NameReference.Create("getMe"))),
                    ExpressionFactory.Readout("u"),
                    Return.Create(IntLiteral.Create("2"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OptionalParameters()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(),
                    IntLiteral.Create("2"), EntityModifier.Reassignable))
                .With(FunctionBuilder.Create(NameDefinition.Create("pass"), new[] {
                        FunctionParameter.Create("v",NameFactory.IntTypeReference(),Variadic.None,
                            FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName, "getme")),isNameRequired:false)
                    },
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                        Return.Create(NameReference.Create("v"))
                    })))
                .With(FunctionBuilder.Create(NameDefinition.Create("getme"), null,
                    ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                        Return.Create(NameReference.Create(NameFactory.ThisVariableName, "x"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("p","pass")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter UsingFunctionParameter()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("inc"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(FunctionCall.Create(NameReference.Create( NameReference.Create("n"),NameFactory.AddOperator),
                    FunctionArgument.Create(IntLiteral.Create("1"))))
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                     Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( IntLiteral.Create("1"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter LocalVariablesLeakCheck()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("last"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("z",null,IntLiteral.Create("2")),
                    Return.Create( NameReference.Create( "z"))
                })));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("pass"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("z",null,IntLiteral.Create("0")),
                    VariableDeclaration.CreateStatement("t",null,FunctionCall.Create( NameReference.Create("last"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("z"),NameReference.Create("t")))
                })));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                     Return.Create( FunctionCall.Create(NameReference.Create("pass")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
