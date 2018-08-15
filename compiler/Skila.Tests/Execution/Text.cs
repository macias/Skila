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
    // http://unicode.mayastudios.com/examples/utf8.html
    // Character Binary code point   Binary UTF-8	Hexadecimal UTF-8
    // $	     U+0024	             00100100	00100100	24
    // ¢	     U+00A2	             00000000 10100010	11000010 10100010	C2 A2
    // €	     U+20AC	             00100000 10101100	11100010 10000010 10101100	E2 82 AC
    // 𤭢	     U+24B62	         00000010 01001011 01100010	11110000 10100100 10101101 10100010	F0 A4 AD A2
    [TestClass]
    public class Text : ITest
    {
        [TestMethod]
        public IInterpreter StringUtf8Encoding()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            NameReference.Create(StringLiteral.Create("$"), NameFactory.StringLength)),

                          ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                             NameReference.Create(StringLiteral.Create("¢"), NameFactory.StringLength)),

                          ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                             NameReference.Create(StringLiteral.Create("€"), NameFactory.StringLength)),

                          ExpressionFactory.AssertEqual(NatLiteral.Create("4"),
                             NameReference.Create(StringLiteral.Create("𤭢"), NameFactory.StringLength)),

                          ExpressionFactory.AssertEqual(NatLiteral.Create("4"),
                             NameReference.Create(StringLiteral.Create("$¢𤭢€"), NameFactory.IIterableCount)),

                          ExpressionFactory.AssertEqual(NatLiteral.Create((System.UInt64)("Les Mise'rables".Length)),
                             NameReference.Create(StringLiteral.Create("Les Mise\u0301rables"), NameFactory.IIterableCount)),

                         Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringRemoving()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create(""),
                            NameFactory.StringRemove), NatLiteral.Create("0"))),

                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringRemove), NatLiteral.Create("0"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringRemove), NatLiteral.Create("3"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("a"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringRemove), NatLiteral.Create("1"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("ac"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringRemove), NatLiteral.Create("1"), NatLiteral.Create("2"))),

                        //https://en.wikipedia.org/wiki/UTF-8#Examples
                         ExpressionFactory.AssertEqual(StringLiteral.Create("€"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€a"),
                            NameFactory.StringRemove), NatLiteral.Create("3"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("€x"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€x"),
                            NameFactory.StringRemove), NatLiteral.Create("3"), NatLiteral.Create("7"))),

                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringSlicing()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create(""),
                            NameFactory.StringSlice), NatLiteral.Create("0"))),

                         ExpressionFactory.AssertEqual(StringLiteral.Create("abc"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringSlice), NatLiteral.Create("0"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringSlice), NatLiteral.Create("3"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("bc"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringSlice), NatLiteral.Create("1"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("b"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("abc"),
                            NameFactory.StringSlice), NatLiteral.Create("1"), NatLiteral.Create("2"))),

                        //https://en.wikipedia.org/wiki/UTF-8#Examples
                         ExpressionFactory.AssertEqual(StringLiteral.Create("a€x"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€x"),
                            NameFactory.StringSlice), NatLiteral.Create("3"))),
                         ExpressionFactory.AssertEqual(StringLiteral.Create("a€"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€x"),
                            NameFactory.StringSlice), NatLiteral.Create("3"), NatLiteral.Create("7"))),

                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringSearching()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                         ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create(""),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'))),
                         ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create("bcd"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'))),

                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a')))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'), NatLiteral.Create("1")))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("5"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'), NatLiteral.Create("2")))),
                         ExpressionFactory.AssertOptionIsNull(FunctionCall.Create(NameReference.Create(StringLiteral.Create("balboa"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'), NatLiteral.Create("6"))),

                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("barbara"),
                            NameFactory.StringIndexOf), StringLiteral.Create("ba")))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("barbara"),
                            NameFactory.StringIndexOf), StringLiteral.Create("ba"), NatLiteral.Create("1")))),

                        //https://en.wikipedia.org/wiki/UTF-8#Examples
                         ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€a"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a')))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("7"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€a"),
                            NameFactory.StringIndexOf), CharLiteral.Create('a'), NatLiteral.Create("4")))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€a"),
                            NameFactory.StringIndexOf), CharLiteral.Create('€')))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("4"),
                             ExpressionFactory.GetOptionValue(FunctionCall.Create(NameReference.Create(StringLiteral.Create("€a€a"),
                            NameFactory.StringIndexOf), CharLiteral.Create('€'), NatLiteral.Create("3")))),

                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter SplittingString()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                        // assert("hello world".split(" ").count() == 2);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create(" ")),
                                NameFactory.IIterableCount))),
                        // assert("hello world".split(" ").at(0) == "hello");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("hello"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create(" ")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),


                        // assert("hello world".split("x").count() == 1);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create("x")),
                                NameFactory.IIterableCount))),
                        // assert("hello world".split("x").at(0) == "hello world");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("hello world"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create("x")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),


                        // assert("hello world".split("h").count() == 2);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create("h")), NameFactory.IIterableCount))),
                        // assert("hello world".split("h").at(0) == "");
                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create("h")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),
                        // assert("hello world".split("h").at(1) == "ello world");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("ello world"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("hello world"), NameFactory.StringSplit),
                                    StringLiteral.Create("h")), NameFactory.AtFunctionName), NatLiteral.Create("1"))),


                        // assert(".".split(".").count() == 2);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("."), NameFactory.StringSplit),
                                    StringLiteral.Create(".")), NameFactory.IIterableCount))),
                        // assert(".".split(".").at(0) == "");
                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("."), NameFactory.StringSplit),
                                    StringLiteral.Create(".")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),
                        // assert(".".split(".").at(1) == "");
                         ExpressionFactory.AssertEqual(StringLiteral.Create(""),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("."), NameFactory.StringSplit),
                                    StringLiteral.Create(".")), NameFactory.AtFunctionName), NatLiteral.Create("1"))),


                        // assert("x".split(".").count() == 1);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("x"), NameFactory.StringSplit),
                                    StringLiteral.Create(".")), NameFactory.IIterableCount))),
                        // assert("x".split(".").at(0) == "x");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("x"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("x"), NameFactory.StringSplit),
                                    StringLiteral.Create(".")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),


                        // assert("x".split("ab").count() == 1);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("x"), NameFactory.StringSplit),
                                    StringLiteral.Create("ab")), NameFactory.IIterableCount))),
                        // assert("x".split("ab").at(0) == "x");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("x"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("x"), NameFactory.StringSplit),
                                    StringLiteral.Create("ab")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),


                        // assert("a-b-c".split("-", limit: 2).count() == 3);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("2")), NameFactory.IIterableCount))),
                        // assert("a-b-c".split("-", limit: 2).at(0) == "a");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("a"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("2")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),
                        // assert("a-b-c".split("-", limit: 2).at(1) == "b");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("b"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("2")), NameFactory.AtFunctionName), NatLiteral.Create("1"))),
                // assert("a-b-c".split("-", limit: 2).at(2) == "c");
                 ExpressionFactory.AssertEqual(StringLiteral.Create("c"),
                  FunctionCall.Create(NameReference.Create(
                    FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                      StringLiteral.Create("-"), NatLiteral.Create("2")), NameFactory.AtFunctionName), NatLiteral.Create("2"))),


                        // assert("a-b-c".split("-", limit: 1).count() == 2);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("1")), NameFactory.IIterableCount))),
                        // assert("a-b-c".split("-", limit: 1).at(0) == "a");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("a"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("1")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),
                        // assert("a-b-c".split("-", limit: 1).at(1) == "b-c");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("b-c"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("a-b-c"), NameFactory.StringSplit),
                                    StringLiteral.Create("-"), NatLiteral.Create("1")), NameFactory.AtFunctionName), NatLiteral.Create("1"))),


                        // assert("𤭢€¢".split("€").count() == 2);
                         ExpressionFactory.AssertEqual(NatLiteral.Create("2"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("𤭢€¢"), NameFactory.StringSplit),
                                    StringLiteral.Create("€")), NameFactory.IIterableCount))),
                        // assert("𤭢€¢".split("€").at(0) == "𤭢");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("𤭢"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("𤭢€¢"), NameFactory.StringSplit),
                                    StringLiteral.Create("€")), NameFactory.AtFunctionName), NatLiteral.Create("0"))),
                        // assert("𤭢€¢".split("€").at(1) == "¢");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("¢"),
                            FunctionCall.Create(NameReference.Create(
                                FunctionCall.Create(NameReference.Create(StringLiteral.Create("𤭢€¢"), NameFactory.StringSplit),
                                    StringLiteral.Create("€")), NameFactory.AtFunctionName), NatLiteral.Create("1"))),

                    Return.Create(Nat8Literal.Create("0"))
                ))
                .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringReversing()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                // https://stackoverflow.com/questions/27331819/whats-the-difference-between-a-character-a-code-point-a-glyph-and-a-grapheme
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
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

                        // assert("Les Mise\u0301rables".reverse()=="selbare\u0301siM seL");
                         ExpressionFactory.AssertEqual(StringLiteral.Create("selbarésiM seL"),
                            FunctionCall.Create(NameReference.Create(StringLiteral.Create("Les Misérables"), NameFactory.StringReverse))),
                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringConversions()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(

                        // assert("345.0" to ?Double==345);
                         ExpressionFactory.AssertEqual(RealLiteral.Create(345),
                             ExpressionFactory.GetOptionValue(
                                FunctionCall.Create(NameReference.Create(NameFactory.RealNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("345.0")))),

                        // assert("1,000" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("1,000"))),

                        // assert("1 000" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("1 000"))),

                        // assert("0" to ?Int==0);
                         ExpressionFactory.AssertEqual(IntLiteral.Create("0"),
                             ExpressionFactory.GetOptionValue(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("0")))),

                        // assert("-0" to ?Int==0);
                         ExpressionFactory.AssertEqual(IntLiteral.Create("-0"),
                             ExpressionFactory.GetOptionValue(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("0")))),

                        // assert("+0" to ?Int==0);
                         ExpressionFactory.AssertEqual(IntLiteral.Create("+0"),
                             ExpressionFactory.GetOptionValue(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("0")))),

                        // assert("abc" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("abc"))),

                        // assert("" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create(""))),

                        // assert("-" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("-"))),

                        // assert("+" to ?Int is null);
                         ExpressionFactory.AssertOptionIsNull(
                                FunctionCall.Create(NameReference.Create(NameFactory.IntNameReference(), NameFactory.ParseFunctionName),
                                StringLiteral.Create("+"))),

                        Return.Create(Nat8Literal.Create("0"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringIterating()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("i", null, Nat8Literal.Create("2"),   env.Options.ReassignableModifier()),
                        // € takes 3 bytes
                        Loop.CreateForEach("ch", NameFactory.CharNameReference(), StringLiteral.Create("€a"),
                        new IExpression[] {
                         ExpressionFactory.IncBy("acc",
                             ExpressionFactory.Mul(NameReference.Create("i"),NameReference.Create("ch",NameFactory.CharLength))),
                         ExpressionFactory.Inc("i")
                        }),

                        Return.Create(NameReference.Create("acc"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)9, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringSearchingBackwards()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
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

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter StringTrimming()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
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

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)0, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithLimits()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                // Skila-1 old test, so in comments there is its syntax used
                // let re = %r/^(?:(\d{4})-)?(?:(\d{1,2})-)?(\d{1,2})$/;
                // let s = "2016-04-14";
                // let matches = re.match(s);
                // assert(matches.count() == 1);
                VariableDeclaration.CreateStatement("re", null,
                             ExpressionFactory.StackConstructor(NameFactory.RegexNameReference(), StringLiteral.Create(@"^(?:(\d{4})-)?(?:(\d{1,2})-)?(\d{1,2})$"))),
                          VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                          VariableDeclaration.CreateStatement("matches", null,
                            FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                           ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                                FunctionCall.Create(NameReference.Create("matches", NameFactory.IIterableCount))),

                            /*
                                assert(matches.at(0).index==0);
                                assert(matches.at(0).count==10);
                                assert(matches.at(0).captures.count==3);
                            */
                            VariableDeclaration.CreateStatement("m", null,
                                FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("0"))),

                             ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("m", NameFactory.MatchEndFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IIterableCount))),

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
                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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
                         ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("7"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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
                         ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
                    ),

                        Return.Create(Nat8Literal.Create("7"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)7, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithNamedCaptures()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                // Skila-1 old test, so in comments there is its syntax used
                // let re = %r/(?<y>\d+)-(?<m>\d+)-(?<d>\d+)/;
                // let s = "2016-04-14";
                // let matches = re.match(s);
                // assert(matches.count() == 1);
                VariableDeclaration.CreateStatement("re", null,
                             ExpressionFactory.StackConstructor(NameFactory.RegexNameReference(), StringLiteral.Create(@"(?<y>\d+)-(?<m>\d+)-(?<d>\d+)"))),
                          VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                          VariableDeclaration.CreateStatement("matches", null,
                            FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                           ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                                FunctionCall.Create(NameReference.Create("matches", NameFactory.IIterableCount))),

                            /*
                                assert(matches.at(0).index==0);
                                assert(matches.at(0).count==10);
                                assert(matches.at(0).captures.count==3);
                            */
                            VariableDeclaration.CreateStatement("m", null,
                                FunctionCall.Create(NameReference.Create("matches", NameFactory.AtFunctionName), NatLiteral.Create("0"))),

                             ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("m", NameFactory.MatchEndFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IIterableCount))),

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
                             ExpressionFactory.GetOptionValue(NameReference.Create("c", NameFactory.CaptureNameFieldName))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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
                             ExpressionFactory.GetOptionValue(NameReference.Create("c", NameFactory.CaptureNameFieldName))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("7"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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
                             ExpressionFactory.GetOptionValue(NameReference.Create("c", NameFactory.CaptureNameFieldName))),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
                    ),

                        Return.Create(Nat8Literal.Create("7"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)7, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexMatchWithAnonymousCaptures()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(
                          // Skila-1 old test, so in comments there is its syntax used
                          // let re = %r/(\d+)/;
                          // let s = "2016-04-14";
                          // let matches = re.match(s);
                          // assert(matches.count() == 3);
                          VariableDeclaration.CreateStatement("re", null,
                             ExpressionFactory.StackConstructor(NameFactory.RegexNameReference(), StringLiteral.Create(@"(\d+)"))),
                          VariableDeclaration.CreateStatement("s", null, StringLiteral.Create("2016-04-14")),
                          VariableDeclaration.CreateStatement("matches", null,
                            FunctionCall.Create(NameReference.Create("re", NameFactory.RegexMatchFunctionName), NameReference.Create("s"))),
                           ExpressionFactory.AssertEqual(NatLiteral.Create("3"),
                                FunctionCall.Create(NameReference.Create("matches", NameFactory.IIterableCount))),

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

                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("m", NameFactory.MatchStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("m", NameFactory.MatchEndFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IIterableCount))),

                        VariableDeclaration.CreateStatement("c", null,
                                FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                    NatLiteral.Create("0"))),
                         ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("0"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("4"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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

                         ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("m", NameFactory.MatchStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("7"), NameReference.Create("m", NameFactory.MatchEndFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IIterableCount))),

                        VariableDeclaration.CreateStatement("c", null,
                                FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                    NatLiteral.Create("0"))),
                         ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("5"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("7"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
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

                         ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("m", NameFactory.MatchStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("m", NameFactory.MatchEndFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("1"),
                            FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.IIterableCount))),

                        VariableDeclaration.CreateStatement("c", null,
                                FunctionCall.Create(NameReference.Create("m", NameFactory.MatchCapturesFieldName, NameFactory.AtFunctionName),
                                    NatLiteral.Create("0"))),
                         ExpressionFactory.AssertOptionIsNull(NameReference.Create("c", NameFactory.CaptureNameFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("8"), NameReference.Create("c", NameFactory.CaptureStartFieldName)),
                         ExpressionFactory.AssertEqual(NatLiteral.Create("10"), NameReference.Create("c", NameFactory.CaptureEndFieldName))
                    ),

                        Return.Create(Nat8Literal.Create("7"))
                    ))
                    .Include(NameFactory.LinqExtensionReference()));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)7, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RegexContains()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DebugThrowOnError = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        // https://msdn.microsoft.com/en-us/library/3y21t6y4(v=vs.110).aspx#Anchor_3
                        VariableDeclaration.CreateStatement("partNumbers", null,
                             ExpressionFactory.StackConstructor(NameFactory.ChunkNameReference(NameFactory.StringPointerNameReference(
                                TypeMutability.ForceConst)), NatLiteral.Create("5"))),
                         ExpressionFactory.InitializeIndexable("partNumbers",
                            StringLiteral.Create("1298-673-4192"), // pass
                            StringLiteral.Create("A08Z-931-468A"), // pass
                            StringLiteral.Create("_A90-123-129X"), // fail
                            StringLiteral.Create("12345-KKA-1230"), // fail
                            StringLiteral.Create("0919-2893-1256")), // fail

                        VariableDeclaration.CreateStatement("rgx", null,
                             ExpressionFactory.StackConstructor(NameFactory.RegexNameReference(),
                                StringLiteral.Create(@"^[a-zA-Z0-9]\d{2}[a-zA-Z0-9](-\d{3}){2}[A-Za-z0-9]$"))),

                        VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), env.Options.ReassignableModifier()),
                        VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("1"), env.Options.ReassignableModifier()),
                        // for each entry we add to the result its (index+1)^2, original code used printing but we need single number
                        Loop.CreateForEach("partNumber",
                            null,
                            NameReference.Create("partNumbers"), new IExpression[] {
                        VariableDeclaration.CreateStatement("w", null,  ExpressionFactory.Mul("i","i")),
                        IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("rgx",NameFactory.RegexContainsFunctionName),
                            NameReference.Create("partNumber")),new IExpression[]{
                                 ExpressionFactory.IncBy("acc","w")
                            }),
                         ExpressionFactory.Inc("i"),
                        }),
                        Return.Create(NameReference.Create("acc"))
                    )));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(5L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}