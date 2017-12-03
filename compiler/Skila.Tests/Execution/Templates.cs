using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Templates
    {
        public IInterpreter TODO_ConditionalConstraint()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Say")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(IntLiteral.Create("2"))
                }))
                .Modifier(EntityModifier.Refines))
                .Parents("ISay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(FunctionCall.Create(NameReference.Create("s","say")))
                }))
                .Parameters(FunctionParameter.Create("s", NameFactory.PointerTypeReference("T")))
                // this is constraint on function, meaning function will be available when T inherits from ISay
                .Constraints(ConstraintBuilder.Create("T").Inherits("ISay"))));


            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("g",null,
                        ExpressionFactory.StackConstructor(NameReference.Create("Greeter",NameReference.Create("Say")))),
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.HeapConstructor(NameReference.Create("Say"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("g","say"),
                        FunctionArgument.Create(NameReference.Create("y"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithPointer()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration(NameDefinition.Create("getMe"),
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
                     ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameFactory.PointerTypeReference("T"), Variadic.None, null, isNameRequired: false)));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(NameDefinition.Create("getMe"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration(NameDefinition.Create("getMe"),
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameReference.Create("T"))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create(NameDefinition.Create("getMe"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.StackConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}