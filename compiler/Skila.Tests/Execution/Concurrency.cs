﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using System.Threading.Tasks;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Concurrency
    {
        [TestMethod]
        public IInterpreter ChannelDeadLockOnSend()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ch",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChannelTypeReference(NameFactory.Int64TypeReference()))),
                    ExpressionFactory.Readout(FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelSend),
                        FunctionArgument.Create(Int64Literal.Create("2")))),
                    Return.Create(Int64Literal.Create("0"))
                    })));

                int task_id = Task.WaitAny(Task.Delay(2000), Task.Run(() => interpreter.TestRun(env, Interpreter.Interpreter.PrepareRun(env))));
                Assert.AreEqual(0, task_id);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ChannelDeadLockOnReceive()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ch",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChannelTypeReference(NameFactory.Int64TypeReference()))),
                    ExpressionFactory.Readout(FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelReceive))),
                    Return.Create(Int64Literal.Create("0"))
                    })));

                int task_id = Task.WaitAny(Task.Delay(2000), Task.Run(() => interpreter.TestRun(env, Interpreter.Interpreter.PrepareRun(env))));
                Assert.AreEqual(0, task_id);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter SingleMessage()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "sender",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    ExpressionFactory.AssertTrue(FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelSend),
                        FunctionArgument.Create(Int64Literal.Create("2")))),
                    }))
                    .Parameters(FunctionParameter.Create("ch", NameFactory.PointerTypeReference(NameFactory.ChannelTypeReference(NameFactory.Int64TypeReference())),
                        Variadic.None, null, isNameRequired: false)));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ch",null,
                        ExpressionFactory.HeapConstructor(NameFactory.ChannelTypeReference(NameFactory.Int64TypeReference()))),
                    Spawn.Create(FunctionCall.Create(NameReference.Create("sender"),FunctionArgument.Create(NameReference.Create("ch")))),
                    VariableDeclaration.CreateStatement("r",null,
                        FunctionCall.Create(NameReference.Create("ch",NameFactory.ChannelReceive))),
                    ExpressionFactory.AssertOptionIsSome(NameReference.Create("r")),
                    Return.Create(ExpressionFactory.GetOptionValue(NameReference.Create("r")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

    }
}
