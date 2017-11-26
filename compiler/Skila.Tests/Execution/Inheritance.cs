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
    public class Inheritance
    {
        [TestMethod]
        public IInterpreter TypeUnion()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("GetPos")
                .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("3"))
                    }))));

            root_ns.AddBuilder(TypeBuilder.Create("GetNeg")
                .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-1"))
                    }))));

            NameReferenceUnion union = NameReferenceUnion.Create(NameFactory.PointerTypeReference(NameReference.Create("GetNeg")),
                NameFactory.PointerTypeReference(NameReference.Create("GetPos")));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",union, Undef.Create(),EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("b",union, Undef.Create(),EntityModifier.Reassignable),
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructorCall("GetPos")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructorCall("GetNeg")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getSome"))),
                    Return.Create(ExpressionFactory.AddOperator(NameReference.Create("x"),NameReference.Create("y")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter TypeIntersection()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetPos")
                .With(FunctionBuilder.CreateDeclaration("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetNeg")
                .With(FunctionBuilder.CreateDeclaration("getMore", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("GetAll")
                .Parents("IGetPos", "IGetNeg")
                .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("3"))
                    }))
                    .Modifier(EntityModifier.Derived)
                    )
                .With(FunctionBuilder.Create("getMore", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-1"))
                    }))
                    .Modifier(EntityModifier.Derived)
                    ));

            NameReferenceIntersection intersection = NameReferenceIntersection.Create(
                NameFactory.PointerTypeReference(NameReference.Create("IGetNeg")),
                NameFactory.PointerTypeReference(NameReference.Create("IGetPos")));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",intersection, Undef.Create(),EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("b",intersection, Undef.Create(),EntityModifier.Reassignable),
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructorCall("GetAll")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructorCall("GetAll")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getMore"))),
                    Return.Create(ExpressionFactory.AddOperator(NameReference.Create("x"),NameReference.Create("y")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter VirtualCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))
                    .Modifier(EntityModifier.Base)));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))
                    .Modifier(EntityModifier.Derived))
                .Parents(NameReference.Create("X")));

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

            return interpreter;
        }
    }
}