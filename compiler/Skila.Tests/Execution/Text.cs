using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Text
    {
        [TestMethod]
        public IInterpreter RegexMatch()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                      // Skila-1 old test, so in comments there is its syntax used
                      // let re = %r/(\d+)/;
                      // let s = "2016-04-14";
                      // let matches = re.match(s);
                      // assert(matches.count() == 3);
                      VariableDeclaration.CreateStatement("re", null,
                        ExpressionFactory.StackConstructor(NameFactory.RegexTypeReference(), StringLiteral.Create(@"(\d+)"))),
                      VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                      VariableDeclaration.CreateStatement("matches", null,
                        FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                      ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.IterableCount))),

                    /*
                      var m = 0;
                      assert(matches.at(m).index==0);
                      assert(matches.at(m).count==4);
                      assert(matches.at(m).captures.count==1);
                      assert(matches.at(m).captures.at(0).id==0); // skipped here
                      assert(matches.at(m).captures.at(0).name is null);
                      assert(matches.at(m).captures.at(0).index==0);
                      assert(matches.at(m).captures.at(0).count==4);
                    */
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("0"))),

                    ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("m", NameFactory.MatchLengthFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                        FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IterableCount))),

                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),
                    ExpressionFactory.AssertOptionValue(NameReference.Create("c", NameFactory.CaptureNameFieldName), hasValue: false),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
                    ++m;
                    assert(matches.at(m).index==5);
                    assert(matches.at(m).count==2);
                    assert(matches.at(m).captures.count==1);
                    assert(matches.at(m).captures.at(0).id==0);
                    assert(matches.at(m).captures.at(0).name is null);
                    assert(matches.at(m).captures.at(0).index==5);
                    assert(matches.at(m).captures.at(0).count==2);
                  */
                    /*
                      ++m;
                      assert(matches.at(m).index==8);
                      assert(matches.at(m).count==2);
                      assert(matches.at(m).captures.count==1);
                      assert(matches.at(m).captures.at(0).id==0);
                      assert(matches.at(m).captures.at(0).name is null);
                      assert(matches.at(m).captures.at(0).index==8);
                      assert(matches.at(m).captures.at(0).count==2);
                     */

                    Return.Create(Int64Literal.Create("7"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(7L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexContains()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    // https://msdn.microsoft.com/en-us/library/3y21t6y4(v=vs.110).aspx#Anchor_3
                    VariableDeclaration.CreateStatement("partNumbers", null,
                        ExpressionFactory.StackConstructor(NameFactory.ChunkTypeReference(NameFactory.StringPointerTypeReference(
                            MutabilityFlag.ForceConst)), NatLiteral.Create("5"))),
                    ExpressionFactory.InitializeIndexable("partNumbers",
                        StringLiteral.Create("1298-673-4192"), // pass
                        StringLiteral.Create("A08Z-931-468A"), // pass
                        StringLiteral.Create("_A90-123-129X"), // fail
                        StringLiteral.Create("12345-KKA-1230"), // fail
                        StringLiteral.Create("0919-2893-1256")), // fail

                    VariableDeclaration.CreateStatement("rgx", null,
                        ExpressionFactory.StackConstructor(NameFactory.RegexTypeReference(),
                            StringLiteral.Create(@"^[a-zA-Z0-9]\d{2}[a-zA-Z0-9](-\d{3}){2}[A-Za-z0-9]$"))),

                    VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("1"), EntityModifier.Reassignable),
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

            Assert.AreEqual(5L, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}