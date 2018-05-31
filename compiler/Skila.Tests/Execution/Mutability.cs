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
    public class Mutability
    {
        [TestMethod]
        public IInterpreter SwapPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));

                VariableDeclaration decl_a = VariableDeclaration.CreateStatement("a", null,
                            ExpressionFactory.HeapConstructor(env.Options.ReassignableTypeMutability(),
                                NameFactory.Nat8TypeReference(),
                                Nat8Literal.Create("2")));

                FunctionCall swap_call = FunctionCall.Create("swap", AddressOf.CreateReference(NameReference.Create("a")),
                            AddressOf.CreateReference(NameReference.Create("b")));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        decl_a,
                        VariableDeclaration.CreateStatement("b", null,
                            ExpressionFactory.HeapConstructor(env.Options.ReassignableTypeMutability(),
                                NameFactory.Nat8TypeReference(),
                                Nat8Literal.Create("17"))),

                        VariableDeclaration.CreateStatement("c", null, NameReference.Create("a")),

                        swap_call,

                        // here `c` still points to value of original `a`

                        Return.Create(ExpressionFactory.Sub("a", "c"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter SwapValuesViaPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetSingleMutability(single_mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null,
                            ExpressionFactory.HeapConstructor(NameFactory.Nat8TypeReference(env.Options.ReassignableTypeMutability()),
                                Nat8Literal.Create("2"))),
                        VariableDeclaration.CreateStatement("b", null,
                            ExpressionFactory.HeapConstructor(NameFactory.Nat8TypeReference(env.Options.ReassignableTypeMutability()),
                                Nat8Literal.Create("17"))),
                        FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b")),
                        Return.Create(ExpressionFactory.Sub("a", "b"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }
     
        [TestMethod]
        public IInterpreter SwapValues()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (bool single_mutability in new[] { true, false })
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }
                    .SetSingleMutability(single_mutability));

                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null, Nat8Literal.Create("2"), env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("b", null, Nat8Literal.Create("17"), env.Options.ReassignableModifier()),
                        FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b")),
                        Return.Create(ExpressionFactory.Sub("a", "b"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ReassigningValues()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("a", NameFactory.Nat8TypeReference(TypeMutability.ForceMutable),
                        Nat8Literal.Create("2")),
                    Assignment.CreateStatement(NameReference.Create("a"), Nat8Literal.Create("13")),
                    Return.Create(NameReference.Create("a"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)13, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}