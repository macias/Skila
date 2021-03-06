﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class Extensions : ITest
    {
        [TestMethod]
        public IInterpreter StaticCallStaticDispatch()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                Extension ext = root_ns.AddNode(Extension.Create("Hq"));

                ext.AddBuilder(FunctionBuilder.Create("paf", NameFactory.Nat8NameReference(), Block.CreateStatement(
                    Return.Create( ExpressionFactory.Mul("x", "x"))))
                    .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference()),
                        EntityModifier.This)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",null,Nat8Literal.Create("5")),
                    Return.Create(FunctionCall.Create(NameReference.Create("Hq","paf"),NameReference.Create("i")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)25, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter InstanceCallStaticDispatch()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                Extension ext = root_ns.AddNode(Extension.Create());

                ext.AddBuilder(FunctionBuilder.Create("paf", NameFactory.Nat8NameReference(), Block.CreateStatement(
                    Return.Create( ExpressionFactory.Mul("x", "x"))))
                    .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference()),
                        EntityModifier.This)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",null,Nat8Literal.Create("5")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","paf")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)25, result.RetValue.PlainValue);
            }

            return interpreter;
        }


    }
}