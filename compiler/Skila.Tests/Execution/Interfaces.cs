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
        public IInterpreter DuckVirtualCallInterface()
        {
          return  duckVirtualCall(new Options() { InterfaceDuckTyping = true });
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
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
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
                    VariableDefiniton.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

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
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))));

            root_ns.AddBuilder(TypeBuilder.Create("Y")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("117"))
                    }))
                    .Modifier(EntityModifier.Base)));

            root_ns.AddBuilder(TypeBuilder.Create("Z")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))
                    .Modifier(EntityModifier.Derived))
                    .Parents(NameReference.Create("Y")));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDefiniton.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("X")),null,EntityModifier.Reassignable),
                    VariableDefiniton.CreateStatement("o",NameFactory.PointerTypeReference(NameReference.Create("Y")),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Z"))),
                    Assignment.CreateStatement(NameReference.Create("i"),NameReference.Create("o")),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
        [TestMethod]
        public IInterpreter DuckVirtualCallWithGenericBaseInterface()
        {
           return  duckVirtualCallWithGenericBase(new Options() { InterfaceDuckTyping = true });
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
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDefiniton.CreateStatement("i",
                        NameFactory.PointerTypeReference(NameReference.Create("X",NameFactory.IntTypeReference())),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Y"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}