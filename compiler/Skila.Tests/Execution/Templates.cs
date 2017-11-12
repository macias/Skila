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
        [TestMethod]
        public void HasConstraint()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionDefinition.CreateDeclaration(EntityModifier.None, NameDefinition.Create("getMe"), null,
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference());
            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None, NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Has(func_constraint).Values), new[] {
                    FunctionParameter.Create("t",NameFactory.PointerTypeReference("T"),Variadic.None,null,isNameRequired:false) },
                     ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     })));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("getMe"),
                    null,
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
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.HeapConstructorCall(NameReference.Create("Y"))),
                    Return.Create(call)
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

    }
}