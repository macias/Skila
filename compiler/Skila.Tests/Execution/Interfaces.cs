using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Interfaces
    {
        [TestMethod]
        public IInterpreter TraitFunctionCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    // e &IEquatable = 3
                    VariableDeclaration.CreateStatement("e",NameFactory.ReferenceTypeReference(NameFactory.IEquatableTypeReference()),
                        Int64Literal.Create("3")),
                    // i Int = 7
                    VariableDeclaration.CreateStatement("i",NameFactory.Int64TypeReference(), Int64Literal.Create("7")),
                    // if e!=i and i!=e then return 2
                    IfBranch.CreateIf(ExpressionFactory.And(ExpressionFactory.IsNotEqual("e","i"),ExpressionFactory.IsNotEqual("e","i")),
                        new[]{ Return.Create(Int64Literal.Create("2")) }),
                    // return 15
                    Return.Create(Int64Literal.Create("15"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DuckVirtualCallInterface()
        {
            return duckVirtualCall(new Options() { InterfaceDuckTyping = true, AllowInvalidMainResult = true,
                DebugThrowOnError = true });
        }

        [TestMethod]
        public IInterpreter DuckVirtualCallProtocol()
        {
            return duckVirtualCall(new Options()
            {
                AllowProtocols = true,
                InterfaceDuckTyping = false,
                DebugThrowOnError = true,
                AllowInvalidMainResult = true
            });
        }

        private IInterpreter duckVirtualCall(Options options)
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(options.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("X")
                    .SetModifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                    .With(FunctionBuilder.Create(
                        "bar",
                        null,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("33"))
                        }))));

                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                    .With(FunctionBuilder.Create("bar",
                        null,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DuckDeepVirtualCallInterface()
        {
            return duckDeepVirtualCall(new Options()
            {
                DebugThrowOnError = true,
                InterfaceDuckTyping = true,
                AllowInvalidMainResult = true
            });
        }

        [TestMethod]
        public IInterpreter DuckDeepVirtualCallProtocol()
        {
            return duckDeepVirtualCall(new Options()
            {
                InterfaceDuckTyping = false,
                AllowProtocols = true,
                DebugThrowOnError = true,
                AllowInvalidMainResult = true
            });
        }

        private IInterpreter duckDeepVirtualCall(Options options)
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(options.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("X")
                    .SetModifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                    .With(FunctionBuilder.Create(
                        "bar",
                        null,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("33"))
                        }))));

                root_ns.AddBuilder(TypeBuilder.Create("Y")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("117"))
                        }))
                        .SetModifier(EntityModifier.Base)));

                root_ns.AddBuilder(TypeBuilder.Create("Z")
                    .With(FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase))
                        .Parents(NameReference.Create("Y")));

                VariableDeclaration decl = VariableDeclaration.CreateStatement("i", NameFactory.PointerTypeReference(NameReference.Create("X")), null,
                        env.Options.ReassignableModifier());

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    decl,
                    VariableDeclaration.CreateStatement("o",NameFactory.PointerTypeReference(NameReference.Create("Y")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Z"))),
                    Assignment.CreateStatement(NameReference.Create("i"),NameReference.Create("o")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DuckVirtualCallWithGenericBaseInterface()
        {
            return duckVirtualCallWithGenericBase(new Options()
            {
                DebugThrowOnError = true,
                InterfaceDuckTyping = true,
                AllowInvalidMainResult = true
            });
        }

        [TestMethod]
        public IInterpreter DuckVirtualCallWithGenericBaseProtocol()
        {
            return duckVirtualCallWithGenericBase(new Options()
            {
                AllowProtocols = true,
                InterfaceDuckTyping = false,
                DebugThrowOnError = true,
                AllowInvalidMainResult = true
            });
        }

        private IInterpreter duckVirtualCallWithGenericBase(Options options)
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(options.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                        .Add("T").Values))
                    .SetModifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                    .With(FunctionBuilder.CreateDeclaration(
                        "bar",
                        ExpressionReadMode.ReadRequired,
                        NameReference.Create("T"))));

                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                    .With(FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64TypeReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",
                        NameFactory.PointerTypeReference(NameReference.Create("X",NameFactory.Int64TypeReference())),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}