﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    // http://unicode.mayastudios.com/examples/utf8.html
    // Character Binary code point   Binary UTF-8	Hexadecimal UTF-8
    // $	     U+0024	             00100100	00100100	24
    // ¢	     U+00A2	             00000000 10100010	11000010 10100010	C2 A2
    // €	     U+20AC	             00100000 10101100	11100010 10000010 10101100	E2 82 AC
    // 𤭢	     U+24B62	         00000010 01001011 01100010	11110000 10100100 10101101 10100010	F0 A4 AD A2
    [TestClass]
    public class Text
    {
        
        [TestMethod]
        public IInterpreter ReversingString()
        {
            // https://stackoverflow.com/questions/27331819/whats-the-difference-between-a-character-a-code-point-a-glyph-and-a-grapheme
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(

                    // assert("hello".reverse()=="olleh");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("olleh"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello"), NameFactory.StringReverse))),

                    // assert("".reverse()=="");
                    ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create(""), NameFactory.StringReverse))),

                    // assert("-".reverse()=="-");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("-"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("-"), NameFactory.StringReverse))),

                    // assert("-+".reverse()=="+-");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("+-"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("-+"), NameFactory.StringReverse))),

                    // assert("123".reverse()=="321");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("321"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("123"), NameFactory.StringReverse))),

                    // assert("$¢€𤭢".reverse()=="𤭢€¢$");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("𤭢€¢$"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("$¢€𤭢"), NameFactory.StringReverse))),

                    // assert("Les Misérables".reverse()=="selbarésiM seL");
                    ExpressionFactory.AssertEqual(StringLiteral.Create("selbarésiM seL"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("Les Misérables"), NameFactory.StringReverse))),
                    Return.Create(Nat8Literal.Create("0"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)0, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringConversions()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(

                    // assert("345.0" to ?Double==345);
                    ExpressionFactory.AssertEqual(RealLiteral.Create(345),
                        ExpressionFactory.GetOptionValue(
                            FunctionCall.Create(NameReference.Create(NameFactory.RealTypeReference(),NameFactory.ParseFunctionName),
                            StringLiteral.Create("345.0")))),

                    // assert("1,000" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("1,000"))),

                    // assert("1 000" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("1 000"))),

                    // assert("0" to ?Int==0);
                    ExpressionFactory.AssertEqual(IntLiteral.Create("0"),
                        ExpressionFactory.GetOptionValue(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("0")))),

                    // assert("-0" to ?Int==0);
                    ExpressionFactory.AssertEqual(IntLiteral.Create("-0"),
                        ExpressionFactory.GetOptionValue(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("0")))),

                    // assert("+0" to ?Int==0);
                    ExpressionFactory.AssertEqual(IntLiteral.Create("+0"),
                        ExpressionFactory.GetOptionValue(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("0")))),

                    // assert("abc" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("abc"))),

                    // assert("" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create(""))),

                    // assert("-" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("-"))),

                    // assert("+" to ?Int is null);
                    ExpressionFactory.AssertOptionIsNull(
                            FunctionCall.Create(NameReference.Create(NameFactory.IntTypeReference(), NameFactory.ParseFunctionName),
                            StringLiteral.Create("+"))),

                    Return.Create(Nat8Literal.Create("0"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)0, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringIterating()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("i", null, Nat8Literal.Create("2"), EntityModifier.Reassignable),
                    // € takes 3 bytes
                    Loop.CreateForEach("ch", NameFactory.CharTypeReference(), StringLiteral.Create("€a"),
                    new IExpression[] {
                        ExpressionFactory.IncBy("acc",
                            ExpressionFactory.Mul(NameReference.Create("i"),NameReference.Create("ch",NameFactory.CharLength))),
                        ExpressionFactory.Inc("i")
                    }),

                    Return.Create(NameReference.Create("acc"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)9, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringSearchingBackwards()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    /*
  assert("".lastIndexOf(%c'a') is null);
  assert("balboa".lastIndexOf(%c'a')==5);
  assert("balboa".lastIndexOf(%c'a',5)==5);
  assert("balboa".lastIndexOf(%c'a',4)==1);
  assert("balboa".lastIndexOf(%c'a',1)==1);
  assert("balboa".lastIndexOf(%c'a',0) is null);
  assert("balboa".lastIndexOf(%c'x') is null);
                     */

                    // please note, reverse indices in Skila-3 are exclusive (in Skila-1 they were inclusive)

                    ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create(""),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'))),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a')))),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'), NatLiteral.Create("6")))),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'), NatLiteral.Create("5")))),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'), NatLiteral.Create("2")))),
                    ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'), NatLiteral.Create("1"))),
                    ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('x'))),

                    //https://en.wikipedia.org/wiki/UTF-8#Examples
                    ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a')))),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                        ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€"),
                        NameFactory.StringLastIndexOf), CharLiteral.Create('a'), NatLiteral.Create("4")))),

                    Return.Create(Nat8Literal.Create("0"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)0, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringTrimming()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    /*
  assert("abc ".trimLeft()=="abc ");
  assert(" abc".trimRight()==" abc");
  assert("abc ".trimRight()=="abc");
  assert("abc  \t".trimRight()=="abc");
  assert(" abc".trimLeft()=="abc");
  assert("\t  abc".trimLeft()=="abc");
                     */

                    ExpressionFactory.AssertEqual(StringLiteral.Create("abc "),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc "), NameFactory.StringTrimStart))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create(" abc"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create(" abc"), NameFactory.StringTrimEnd))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc "), NameFactory.StringTrimEnd))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc  \t"), NameFactory.StringTrimEnd))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create(" abc"), NameFactory.StringTrimStart))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("\t  abc"), NameFactory.StringTrimStart))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("€"),
                        FunctionCall.Create(NameReference.Create(StringLiteral.Create("\t  €"), NameFactory.StringTrimStart))),

                    Return.Create(Nat8Literal.Create("0"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)0, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithLimits()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
            // Skila-1 old test, so in comments there is its syntax used
            // let re = %r/^(?:(\d{4})-)?(?:(\d{1,2})-)?(\d{1,2})$/;
            // let s = "2016-04-14";
            // let matches = re.match(s);
            // assert(matches.count() == 1);
            VariableDeclaration.CreateStatement("re", null,
                        ExpressionFactory.StackConstructor(NameFactory.RegexTypeReference(), StringLiteral.Create(@"^(?:(\d{4})-)?(?:(\d{1,2})-)?(\d{1,2})$"))),
                      VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                      VariableDeclaration.CreateStatement("matches", null,
                        FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                      ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.IterableCount))),

                        /*
                            assert(matches.at(0).index==0);
                            assert(matches.at(0).count==10);
                            assert(matches.at(0).captures.count==3);
                        */
                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("0"))),

                        ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("m", NameFactory.MatchLengthFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                        FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IterableCount))),

                        /*
                            assert(matches.at(0).captures.at(0).id==0); // skipped here
                            assert(matches.at(0).captures.at(0).name is null);
                            assert(matches.at(0).captures.at(0).index==0);
                            assert(matches.at(0).captures.at(0).count==4);
                        */
                        Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
    assert(matches.at(0).captures.at(1).id==1);  // skipped here
    assert(matches.at(0).captures.at(1).name is null);
    assert(matches.at(0).captures.at(1).index==5);
    assert(matches.at(0).captures.at(1).count==2);
                  */
                    Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("1"))),
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
    assert(matches.at(0).captures.at(2).id==2);   // skipped here
    assert(matches.at(0).captures.at(2).name is null);
    assert(matches.at(0).captures.at(2).index==8);
    assert(matches.at(0).captures.at(2).count==2);
    */
                    Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("2"))),
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),

                    Return.Create(Nat8Literal.Create("7"))
                ))
                .Include(NameFactory.LinqExtensionReference()));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)7, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithNamedCaptures()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
            // Skila-1 old test, so in comments there is its syntax used
            // let re = %r/(?<y>\d+)-(?<m>\d+)-(?<d>\d+)/;
            // let s = "2016-04-14";
            // let matches = re.match(s);
            // assert(matches.count() == 1);
            VariableDeclaration.CreateStatement("re", null,
                        ExpressionFactory.StackConstructor(NameFactory.RegexTypeReference(), StringLiteral.Create(@"(?<y>\d+)-(?<m>\d+)-(?<d>\d+)"))),
                      VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                      VariableDeclaration.CreateStatement("matches", null,
                        FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                      ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.IterableCount))),

                        /*
                            assert(matches.at(0).index==0);
                            assert(matches.at(0).count==10);
                            assert(matches.at(0).captures.count==3);
                        */
                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("0"))),

                        ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("m", NameFactory.MatchLengthFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                        FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IterableCount))),

                        /*
                            assert(matches.at(0).captures.at(0).id==0); // skipped here
                            assert(matches.at(0).captures.at(0).name=="y");
                            assert(matches.at(0).captures.at(0).index==0);
                            assert(matches.at(0).captures.at(0).count==4);
                        */
                        Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("y"),
                        NameReference.Create("c", NameFactory.CaptureNameFieldName, NameFactory.OptionValue)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
    assert(matches.at(0).captures.at(1).id==1);  // skipped here
    assert(matches.at(0).captures.at(1).name=="m");
    assert(matches.at(0).captures.at(1).index==5);
    assert(matches.at(0).captures.at(1).count==2);
                  */
                    Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("1"))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("m"),
                        NameReference.Create("c", NameFactory.CaptureNameFieldName, NameFactory.OptionValue)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
    assert(matches.at(0).captures.at(2).id==2);   // skipped here
    assert(matches.at(0).captures.at(2).name=="d");
    assert(matches.at(0).captures.at(2).index==8);
    assert(matches.at(0).captures.at(2).count==2);
    */
                    Block.CreateStatement(
                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("2"))),
                    ExpressionFactory.AssertEqual(StringLiteral.Create("d"),
                        NameReference.Create("c", NameFactory.CaptureNameFieldName, NameFactory.OptionValue)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),

                    Return.Create(Nat8Literal.Create("7"))
                ))
                .Include(NameFactory.LinqExtensionReference()));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)7, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithAnonymousCaptures()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
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
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
                    ++m;
                    assert(matches.at(m).index==5);
                    assert(matches.at(m).count==2);
                    assert(matches.at(m).captures.count==1);
                    assert(matches.at(m).captures.at(0).id==0); // skipped here
                    assert(matches.at(m).captures.at(0).name is null);
                    assert(matches.at(m).captures.at(0).index==5);
                    assert(matches.at(m).captures.at(0).count==2);
                  */
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("1"))),

                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("m", NameFactory.MatchIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("m", NameFactory.MatchLengthFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                        FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IterableCount))),

                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),
                    /*
                      ++m;
                      assert(matches.at(m).index==8);
                      assert(matches.at(m).count==2);
                      assert(matches.at(m).captures.count==1);
                      assert(matches.at(m).captures.at(0).id==0); // skipped here
                      assert(matches.at(m).captures.at(0).name is null);
                      assert(matches.at(m).captures.at(0).index==8);
                      assert(matches.at(m).captures.at(0).count==2);
                     */
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("m", null,
                            FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("2"))),

                    ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("m", NameFactory.MatchIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("m", NameFactory.MatchLengthFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                        FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IterableCount))),

                    VariableDeclaration.CreateStatement("c", null,
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                NatLiteral.Create("0"))),
                    ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureIndexFieldName)),
                    ExpressionFactory.AssertEqual(NatLiteral.Create("2"), NameReference.Create("c", NameFactory.CaptureLengthFieldName))
                ),

                    Return.Create(Nat8Literal.Create("7"))
                ))
                .Include(NameFactory.LinqExtensionReference()));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)7, result.RetValue.PlainValue);

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
                            MutabilityOverride.ForceConst)), NatLiteral.Create("5"))),
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
                    Loop.CreateForEach("partNumber",
                        null, 
                        NameReference.Create("partNumbers"), new IExpression[] {
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