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
    public class ObjectInitialization
    {
        [TestMethod]
        public IInterpreter InitializingWithCustomSetter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }.SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.Create(env.Options, "x", NameFactory.Nat8TypeReference())
                        .With(VariableDeclaration.CreateStatement("f", NameFactory.Nat8TypeReference(), null, 
                            env.Options.ReassignableModifier() ))
                        .WithGetter(Block.CreateStatement(Return.Create(NameReference.Create("f"))))
                        .WithSetter(Block.CreateStatement(Assignment.CreateStatement(NameReference.Create("f"),
                            ExpressionFactory.Mul(NameFactory.PropertySetterValueReference(), Nat8Literal.Create("2")))))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
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

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(PropertyBuilder.CreateAutoGetter(env.Options, "x", NameFactory.Nat8TypeReference())
                        .SetModifier(EntityModifier.PostInitialization))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("5"))
                        ))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
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

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(PropertyBuilder.CreateAutoGetter(env.Options, "x", NameFactory.Nat8TypeReference()))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("5"))
                        ))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)5, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
