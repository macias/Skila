﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Concurrency
    {
        [TestMethod]
        public void BasicChannel()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ch",null,
                        ExpressionFactory.HeapConstructorCall(NameFactory.ChannelTypeReference(NameFactory.IntTypeReference()))),
                    ExpressionFactory.AssertTrue(FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelSend),
                        FunctionArgument.Create(IntLiteral.Create("2")))),
                    VariableDeclaration.CreateStatement("r",null,
                        FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelReceive))),
                    ExpressionFactory.AssertOptionValue(NameReference.Create("r")),
                    Return.Create(NameReference.Create("r",NameFactory.OptionValue))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void SingleMessage()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("sender"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.AssertTrue(FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelSend),
                        FunctionArgument.Create(IntLiteral.Create("2")))),
                }))
                .Parameters(FunctionParameter.Create("ch", NameFactory.PointerTypeReference(NameFactory.ChannelTypeReference(NameFactory.IntTypeReference())),
                    Variadic.None, null, isNameRequired: false)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ch",null,
                        ExpressionFactory.HeapConstructorCall(NameFactory.ChannelTypeReference(NameFactory.IntTypeReference()))),
                    Spawn.Create(FunctionCall.Create(NameReference.Create("sender"),FunctionArgument.Create(NameReference.Create("ch")))),
                    VariableDeclaration.CreateStatement("r",null,
                        FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelReceive))),
                    ExpressionFactory.AssertOptionValue(NameReference.Create("r")),
                    Return.Create(NameReference.Create("r",NameFactory.OptionValue))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

    }
}
