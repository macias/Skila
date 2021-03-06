﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class Io : ITest
    {
        private const string randomTextFilePath = "Data/random_text.utf8.txt";

        [TestMethod]
        public IInterpreter CommandLine()
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
                         ExpressionFactory.AssertEqual(StringLiteral.Create(Interpreter.Interpreter.CommandLineTestProgramPath),
                            NameReference.Create(NameFactory.CommandLineProgramPath)),

                         ExpressionFactory.AssertEqual(StringLiteral.Create(Interpreter.Interpreter.CommandLineTestArgument),
                            FunctionCall.Create(NameReference.Create(NameFactory.CommandLineArguments, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),

                        Return.Create(Nat8Literal.Create("0"))
                    )).
                    Parameters(FunctionParameter.Create(NameFactory.CommandLineProgramPath, 
                        NameFactory.StringPointerNameReference(TypeMutability.ReadOnly)),
                        FunctionParameter.Create(NameFactory.CommandLineArguments,
                            NameFactory.StringPointerNameReference(TypeMutability.ReadOnly), Variadic.Create(), null, isNameRequired: false)));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter FileExists()
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
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("e", null, FunctionCall.Create(NameReference.Create(NameFactory.FileNameReference(),
                            NameFactory.FileExists), StringLiteral.Create(randomTextFilePath))),
                        IfBranch.CreateIf(NameReference.Create("e"), new[] { Return.Create(Int64Literal.Create("2")) },
                            IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("-5")) }))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter FileReadingLines()
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
                    NameFactory.Nat64NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("lines", null,
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(NameFactory.FileNameReference(), NameFactory.FileReadLines),
                                StringLiteral.Create(randomTextFilePath)))),
                        // first line is "It was" (without quotes)
                        VariableDeclaration.CreateStatement("first", null,
                            FunctionCall.Create(NameReference.Create("lines", NameFactory.PropertyIndexerName), NatLiteral.Create("0"))),
                        // 6
                        VariableDeclaration.CreateStatement("len", null,
                            NameReference.Create("first", NameFactory.IIterableCount)),
                        Return.Create(NameReference.Create("len"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(6UL, result.RetValue.PlainValue);
            }

            return interpreter;
        }

    }
}