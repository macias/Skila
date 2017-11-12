using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Interfaces
    {
        [TestMethod]
        public void DuckVirtualCallInterface()
        {
            duckVirtualCall(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public void DuckVirtualCallProtocol()
        {
            duckVirtualCall(new Options() { InterfaceDuckTyping = false });
        }
        private void duckVirtualCall(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .Modifier(options.InterfaceDuckTyping? EntityModifier.Interface: EntityModifier.Protocol)
                .With(FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DuckDeepVirtualCallInterface()
        {
            duckDeepVirtualCall(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public void DuckDeepVirtualCallProtocol()
        {
            duckDeepVirtualCall(new Options() { InterfaceDuckTyping = false });
        }

        private void duckDeepVirtualCall(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                .With(FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))));

            root_ns.AddBuilder(TypeBuilder.Create("Y")
                .Modifier(EntityModifier.Base)
                .With(FunctionDefinition.CreateFunction(EntityModifier.Base,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("117"))
                    }))));

            root_ns.AddBuilder(TypeBuilder.Create("Z")
                .With(FunctionDefinition.CreateFunction(EntityModifier.Derived,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    })))
                    .Parents(NameReference.Create("Y")));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),null,EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("o",NameFactory.PointerTypeReference(NameReference.Create("Y")),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Z"))),
                    Assignment.CreateStatement(NameReference.Create("i"),NameReference.Create("o")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }
        [TestMethod]
        public void DuckVirtualCallWithGenericBaseInterface()
        {
            duckVirtualCallWithGenericBase(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public void DuckVirtualCallWithGenericBaseProtocol()
        {
            duckVirtualCallWithGenericBase(new Options() { InterfaceDuckTyping = false });
        }

        private void duckVirtualCallWithGenericBase(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                    .Add("T").Values))
                .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol)
                .With(FunctionDefinition.CreateDeclaration(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameReference.Create("T"))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    null,
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",
                        NameFactory.PointerTypeReference(NameReference.Create("X",NameFactory.IntTypeReference())),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }
    }
}