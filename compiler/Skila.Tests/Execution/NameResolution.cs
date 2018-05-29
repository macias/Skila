using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Expressions.Literals;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class NameResolution
    {
        [TestMethod]
        public IInterpreter GenericTypeAliasing()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point", "V", VarianceMode.None))
                .With(Alias.Create("VType", NameReference.Create("V"), EntityModifier.Public)));

            VariableDeclaration decl = VariableDeclaration.CreateStatement("x", NameReference.Create("p", "VType"), Undef.Create(), EntityModifier.Reassignable);
            root_ns.AddBuilder(FunctionBuilder.Create("main", NameFactory.Nat8TypeReference(), Block.CreateStatement(
                    VariableDeclaration.CreateStatement("p", null,
                        ExpressionFactory.StackConstructor(NameReference.Create("Point", NameFactory.Nat8TypeReference()))),
                    decl,
                    Assignment.CreateStatement(NameReference.Create("x"), Nat8Literal.Create("5")),
                    Return.Create(NameReference.Create("x"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)5, result.RetValue.PlainValue);

            return interpreter;
        }

    }
}