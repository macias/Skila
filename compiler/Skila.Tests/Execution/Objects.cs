using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Objects
    {
        [TestMethod]
        public IInterpreter ParallelAssignment()
        {
            var env = Language.Environment.Create(new Options() { ThrowOnError = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,IntLiteral.Create("-5"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",null,IntLiteral.Create("2"), EntityModifier.Reassignable),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y") },
                        new[]{ NameReference.Create("y"),NameReference.Create("x") }),
                    Return.Create(NameReference.Create("x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AccessingObjectFields()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(VariableDeclaration.CreateStatement("y", NameFactory.IntTypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     IntLiteral.Create("2")),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter UsingEnums()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateEnum("Size")
                .With(EnumCaseBuilder.Create("small", "big")));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s",null,NameReference.Create("Size","small")),
                    IfBranch.CreateIf(ExpressionFactory.IsNotEqual(NameReference.Create( "s"),NameReference.Create("Size","big")),
                        new[]{ Return.Create(IntLiteral.Create("2")) }),
                    Return.Create(IntLiteral.Create("5"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ConstructorChaining()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition base_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                Block.CreateStatement(new[] {
                    // a = a + 5   --> 4
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, "a"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"a"),IntLiteral.Create("5")))
                }));
            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(base_constructor)
                .With(VariableDeclaration.CreateStatement("a", NameFactory.IntTypeReference(), IntLiteral.Create("-1"),
                    EntityModifier.Public | EntityModifier.Reassignable)));

            FunctionDefinition next_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                Block.CreateStatement(new[] {
                    // b = b + 15 --> +5
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"b"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"b"),IntLiteral.Create("15")))
                }), ExpressionFactory.BaseInit());

            TypeDefinition next_type = root_ns.AddBuilder(TypeBuilder.Create("Next")
                .Parents("Point")
                .Modifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(next_constructor)

                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                    new[] { FunctionParameter.Create("i", NameFactory.IntTypeReference()) },
                    Block.CreateStatement(new[] {
                    // b = b + i  --> i+5
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"b"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"b"),NameReference.Create("i")))
                }), ExpressionFactory.ThisInit()))
                .With(VariableDeclaration.CreateStatement("b", NameFactory.IntTypeReference(), IntLiteral.Create("-10"),
                    EntityModifier.Public | EntityModifier.Reassignable)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Next"),
                        FunctionArgument.Create(IntLiteral.Create("-7")))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("p","a"), NameReference.Create("p","b")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
