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
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructor("GetPos")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructor("GetNeg")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getSome"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("x"),NameReference.Create("y")))
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
                    .Modifier(EntityModifier.Refines)
                    )
                .With(FunctionBuilder.Create("getMore", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-1"))
                    }))
                    .Modifier(EntityModifier.Refines)
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
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructor("GetAll")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructor("GetAll")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getMore"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("x"),NameReference.Create("y")))
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

            root_ns.AddBuilder(TypeBuilder.Create("MyBase")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("33"))
                    }))
                    .Modifier(EntityModifier.Base)));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("SomeChild")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))
                    .Modifier(EntityModifier.Refines | EntityModifier.UnchainBase))
                .Parents(NameReference.Create("MyBase")));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("MyBase")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("SomeChild"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter VirtualCallAtBase()
        {
            var env = Environment.Create(new Options() { BaseReferenceEnabled = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IBase")
                .With(FunctionBuilder.CreateDeclaration("getA", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference()))
                .With(FunctionBuilder.CreateDeclaration("getB", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .Parents("IBase")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getA"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-50"))
                    }))
                    .Modifier(EntityModifier.Refines))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("51"))
                    }))
                    .Modifier(EntityModifier.Refines)));

            root_ns.AddBuilder(TypeBuilder.Create("End")
                .Parents("Middle")
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getA"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-1000"))
                    }))
                    .Modifier(EntityModifier.Refines | EntityModifier.UnchainBase))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("getB"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        // return 1+super()+base.getA()
                        Return.Create(ExpressionFactory.Add( IntLiteral.Create("1"),
                            ExpressionFactory.Add(FunctionCall.Create(NameReference.Create(NameFactory.SuperFunctionName)),
                                FunctionCall.Create(NameReference.Create(NameFactory.BaseVariableName,"getA")))))
                    }))
                    .Modifier(EntityModifier.Refines)));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // i *IBase
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("IBase")),
                        null,EntityModifier.Reassignable),
                    // i = new End()
                    Assignment.CreateStatement(NameReference.Create("i"),
                        ExpressionFactory.HeapConstructor(NameReference.Create("End"))),
                    // return i.getB()
                    Return.Create(FunctionCall.Create(NameReference.Create("i","getB")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}