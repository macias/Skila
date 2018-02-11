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
            var env = Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // e &IEquatable = 3
                    VariableDeclaration.CreateStatement("e",NameFactory.ReferenceTypeReference(NameFactory.IEquatableTypeReference()),
                        Int64Literal.Create("3")),
                    // i Int = 7
                    VariableDeclaration.CreateStatement("i",NameFactory.Int64TypeReference(), Int64Literal.Create("7")),
                    // if e!=i and i!=e then return 2
                    IfBranch.CreateIf(ExpressionFactory.And(ExpressionFactory.NotEqual("e","i"),ExpressionFactory.NotEqual("e","i")),
                        new[]{ Return.Create(Int64Literal.Create("2")) }),
                    // return 15
                    Return.Create(Int64Literal.Create("15"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DuckVirtualCallInterface()
        {
            return duckVirtualCall(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public IInterpreter DuckVirtualCallProtocol()
        {
            return duckVirtualCall(new Options() { InterfaceDuckTyping = false });
        }
        private IInterpreter duckVirtualCall(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("33"))
                    }))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DuckDeepVirtualCallInterface()
        {
            return duckDeepVirtualCall(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public IInterpreter DuckDeepVirtualCallProtocol()
        {
            return duckDeepVirtualCall(new Options() { InterfaceDuckTyping = false });
        }

        private IInterpreter duckDeepVirtualCall(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("33"))
                    }))));

            root_ns.AddBuilder(TypeBuilder.Create("Y")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("117"))
                    }))
                    .Modifier(EntityModifier.Base)));

            root_ns.AddBuilder(TypeBuilder.Create("Z")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase))
                    .Parents(NameReference.Create("Y")));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),null,EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("o",NameFactory.PointerTypeReference(NameReference.Create("Y")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Z"))),
                    Assignment.CreateStatement(NameReference.Create("i"),NameReference.Create("o")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }
        [TestMethod]
        public IInterpreter DuckVirtualCallWithGenericBaseInterface()
        {
            return duckVirtualCallWithGenericBase(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public IInterpreter DuckVirtualCallWithGenericBaseProtocol()
        {
            return duckVirtualCallWithGenericBase(new Options() { InterfaceDuckTyping = false });
        }

        private IInterpreter duckVirtualCallWithGenericBase(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                    .Add("T").Values))
                .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameReference.Create("T"))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",
                        NameFactory.PointerTypeReference(NameReference.Create("X",NameFactory.Int64TypeReference())),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}