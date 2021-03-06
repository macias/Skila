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
    public class ObjectInitialization : ITest
    {
        [TestMethod]
        public IInterpreter NoExtrasWithCopyConstructor()
        {
            // nothing is written in stone, but for now let's treat assignment in declaration as assignment
            // not copy constructor (as in C++)
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement()))
                    // copy-constructor
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"),Nat8Literal.Create("66"))
                        )).Parameters(FunctionParameter.Create("p", NameFactory.ReferenceNameReference("Point"),
                            ExpressionReadMode.CannotBeRead)))

                    .With(PropertyBuilder.Create(env.Options, "x", () => NameFactory.Nat8NameReference())
                        .With(VariableDeclaration.CreateStatement("f", NameFactory.Nat8NameReference(), null,
                            env.Options.ReassignableModifier()))
                        .WithGetter(Block.CreateStatement(Return.Create(NameReference.CreateThised("f"))))
                        .WithSetter(Block.CreateStatement(Assignment.CreateStatement(NameReference.CreateThised("f"),
                             ExpressionFactory.Mul(NameFactory.PropertySetterValueReference(), Nat8Literal.Create("2")))))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,
                        ConstructorCall.StackConstructor(NameReference.Create("Point"))
                        .Init("x",Nat8Literal.Create("7"))
                        .Build()),
                    // bit-copy of the object, there is no calling copy-constructor here
                    VariableDeclaration.CreateStatement("r",null,NameReference.Create("p")),
                    Return.Create(NameReference.Create(NameReference.Create("r"),"x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)14, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter InitializingWithCustomSetter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.Create(env.Options, "x", () => NameFactory.Nat8NameReference())
                        .With(VariableDeclaration.CreateStatement("f", NameFactory.Nat8NameReference(), null,
                            env.Options.ReassignableModifier()))
                        .WithGetter(Block.CreateStatement(Return.Create(NameReference.CreateThised("f"))))
                        .WithSetter(Block.CreateStatement(Assignment.CreateStatement(NameReference.CreateThised("f"),
                             ExpressionFactory.Mul(NameFactory.PropertySetterValueReference(), Nat8Literal.Create("2")))))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,
                        ConstructorCall.StackConstructor(NameReference.Create("Point"))
                        .Init("x",Nat8Literal.Create("7"))
                        .Build()),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)14, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter InitializingWithObjectInitialization()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(PropertyBuilder.CreateAutoGetter(env.Options, "x", NameFactory.Nat8NameReference())
                        .SetModifier(EntityModifier.PostInitialization))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("5"))
                        ))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,
                        ConstructorCall.StackConstructor(NameReference.Create("Point"))
                        .Init("x",Nat8Literal.Create("17"))
                        .Build()),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)17, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter InitializingWithGetter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(PropertyBuilder.CreateAutoGetter(env.Options, "x", NameFactory.Nat8NameReference()))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("5"))
                        ))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null, ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)5, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
