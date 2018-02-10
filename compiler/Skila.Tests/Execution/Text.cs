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
    public class Text
    {
        [TestMethod]
        public IInterpreter RegexContains()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(
                    // https://msdn.microsoft.com/en-us/library/3y21t6y4(v=vs.110).aspx#Anchor_3
                    VariableDeclaration.CreateStatement("partNumbers",null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkTypeReference(NameFactory.StringPointerTypeReference(
                            MutabilityFlag.ForceConst)),IntLiteral.Create("5"))),
                    ExpressionFactory.InitializeIndexable("partNumbers",
                        StringLiteral.Create("1298-673-4192"), // pass
                        StringLiteral.Create("A08Z-931-468A"), // pass
                        StringLiteral.Create("_A90-123-129X"), // fail
                        StringLiteral.Create("12345-KKA-1230"), // fail
                        StringLiteral.Create("0919-2893-1256")), // fail

                    VariableDeclaration.CreateStatement("rgx",null,
                        ExpressionFactory.StackConstructor(NameFactory.RegexTypeReference(),
                            StringLiteral.Create(@"^[a-zA-Z0-9]\d{2}[a-zA-Z0-9](-\d{3}){2}[A-Za-z0-9]$"))),

                    VariableDeclaration.CreateStatement("acc",null,IntLiteral.Create("0"),EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("i", null, IntLiteral.Create("1"), EntityModifier.Reassignable),
                    // for each entry we add to the result its (index+1)^2, original code used printing but we need single number
                    Loop.CreateForEach("partNumber", null, NameReference.Create("partNumbers"), new IExpression[] {
                        VariableDeclaration.CreateStatement("w", null, ExpressionFactory.Mul("i","i")),
                        IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("rgx",NameFactory.RegexContainsFunctionName),
                            NameReference.Create("partNumber")),new IExpression[]{
                                ExpressionFactory.IncBy("acc","w")
                            }),
                        ExpressionFactory.Inc("i"),
                    }),
                    Return.Create(NameReference.Create("acc"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(5, result.RetValue.PlainValue);

            return interpreter;
        }


    }
}