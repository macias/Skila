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
    public class Mutability : ITest
    {
        [TestMethod]
        public IInterpreter OldSchoolSwapPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowDereference = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceNameReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceNameReference("T"))));

                VariableDeclaration decl_a = VariableDeclaration.CreateStatement("a", null,
                             ExpressionFactory.HeapConstructor(env.Options.ReassignableTypeMutability(),
                                NameFactory.Nat8NameReference(),
                                Nat8Literal.Create("2")));

                FunctionCall swap_call = FunctionCall.Create("swap", AddressOf.CreateReference(NameReference.Create("a")),
                            AddressOf.CreateReference(NameReference.Create("b")));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        decl_a,
                        VariableDeclaration.CreateStatement("b", null,
                             ExpressionFactory.HeapConstructor(env.Options.ReassignableTypeMutability(),
                                NameFactory.Nat8NameReference(),
                                Nat8Literal.Create("17"))),

                        VariableDeclaration.CreateStatement("c", null, NameReference.Create("a")),

                        swap_call,

                        // here `c` still points to value of original `a`

                        Return.Create( ExpressionFactory.Sub("a", "c"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OldSchoolSwapValuesViaPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowDereference = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceNameReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceNameReference("T"))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null,
                             ExpressionFactory.HeapConstructor(NameFactory.Nat8NameReference(env.Options.ReassignableTypeMutability()),
                                Nat8Literal.Create("2"))),
                        VariableDeclaration.CreateStatement("b", null,
                             ExpressionFactory.HeapConstructor(NameFactory.Nat8NameReference(env.Options.ReassignableTypeMutability()),
                                Nat8Literal.Create("17"))),
                        FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b")),
                        Return.Create( ExpressionFactory.Sub("a", "b"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }
     
        [TestMethod]
        public IInterpreter OldSchoolSwapValues()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowDereference =true }
                    .SetMutability(mutability));

                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceNameReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceNameReference("T"))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", null, Nat8Literal.Create("2"), env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("b", null, Nat8Literal.Create("17"), env.Options.ReassignableModifier()),
                        FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b")),
                        Return.Create( ExpressionFactory.Sub("a", "b"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)15, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        //[TestMethod]
        public IInterpreter OBSOLETE_ReassigningValues()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true }
                .SetMutability(MutabilityModeOption.SingleMutability));
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8NameReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("a", NameFactory.Nat8NameReference(TypeMutability.ForceMutable),
                        Nat8Literal.Create("2")),
                    Assignment.CreateStatement(NameReference.Create("a"), Nat8Literal.Create("13")),
                    Return.Create(NameReference.Create("a"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)13, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter NoMutability()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true }
                .SetMutability(MutabilityModeOption.OnlyAssignability));
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8NameReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("a", NameFactory.Nat8NameReference(TypeMutability.ForceMutable),
                        Nat8Literal.Create("2"), env.Options.ReassignableModifier()),
                    VariableDeclaration.CreateStatement("b", NameFactory.Nat8NameReference(TypeMutability.ForceConst),
                        Nat8Literal.Create("13")),
                    Assignment.CreateStatement(NameReference.Create("a"), NameReference.Create("b")),
                    Return.Create(NameReference.Create("a"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)13, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}