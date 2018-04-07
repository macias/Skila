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
    public class Objects
    {
        [TestMethod]
        public IInterpreter CallingImplicitConstMethodOnHeapOnlyPointer()
        {
            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                DebugThrowOnError = true
            });
            var root_ns = env.Root;


            TypeDefinition type_def = root_ns.AddBuilder(TypeBuilder.Create("Carbon")
                .SetModifier(EntityModifier.Mutable | EntityModifier.HeapOnly)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Nat8TypeReference(), null, EntityModifier.Public | EntityModifier.Reassignable))

                // default constructor
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("7")))))

                // copy constructor
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("7"))))
                    .Parameters(FunctionParameter.Create("cp",
                        NameFactory.ReferenceTypeReference(NameFactory.SelfTypeReference(MutabilityOverride.Neutral)),
                            ExpressionReadMode.CannotBeRead)))

                // this is a mutable method
                .With(FunctionBuilder.Create("turtle", NameFactory.UnitTypeReference(), Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("13"))
                    ))
                    .SetModifier(EntityModifier.Mutable))

                            );

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.HeapConstructor("Carbon")),

                    // calling const-method (which currently is auto created)
                    VariableDeclaration.CreateStatement("m",null,FunctionCall.Create(NameReference.Create("a","turtle"))),

                    VariableDeclaration.CreateStatement("r",null,
                        ExpressionFactory.Sub(NameReference.Create("m","x"),NameReference.Create("a","x"))),

                        Return.Create(NameReference.Create("r"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)6, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingImplicitConstMethodOnValueOnHeap()
        {
            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                DebugThrowOnError = true
            });
            var root_ns = env.Root;


            TypeDefinition type_def = root_ns.AddBuilder(TypeBuilder.Create("Carbon")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Nat8TypeReference(), null, EntityModifier.Public | EntityModifier.Reassignable))

                // this is a mutable method
                .With(FunctionBuilder.Create("turtle", NameFactory.UnitTypeReference(), Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("13"))
                    ))
                    .SetModifier(EntityModifier.Mutable))

                            );

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.HeapConstructor("Carbon")),

                    // calling const-method (which currently is auto created)
                    VariableDeclaration.CreateStatement("m",null,FunctionCall.Create(NameReference.Create("a","turtle"))),

                    VariableDeclaration.CreateStatement("r",null,
                        ExpressionFactory.Sub(NameReference.Create("m","x"),NameReference.Create("a","x"))),

                        Return.Create(NameReference.Create("r"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)13, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingImplicitConstMethodOnValueOnStack()
        {
            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                DebugThrowOnError = true
            });
            var root_ns = env.Root;


            TypeDefinition type_def = root_ns.AddBuilder(TypeBuilder.Create("Carbon")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Nat8TypeReference(), null, EntityModifier.Public | EntityModifier.Reassignable))

                // this is a mutable method
                .With(FunctionBuilder.Create("turtle",NameFactory.UnitTypeReference(),Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"),Nat8Literal.Create("13"))
                    ))
                    .SetModifier(EntityModifier.Mutable))

                            );

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.StackConstructor("Carbon")),

                    // calling const-method (which currently is auto created)
                    VariableDeclaration.CreateStatement("m",null,FunctionCall.Create(NameReference.Create("a","turtle"))),

                    VariableDeclaration.CreateStatement("r",null,
                        ExpressionFactory.Sub(NameReference.Create("m","x"),NameReference.Create("a","x"))),

                        Return.Create(NameReference.Create("r"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)13, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RuntimeSelfTypeResolutionOnExternalObject()
        {
            // the purpose of this test is to check whether `Self` is correctly resolved and appropriate constructor is called
            // when used on external object

            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                DebugThrowOnError = true
            });
            var root_ns = env.Root;


            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("IDuplicate"))

                                .With(FunctionBuilder.CreateInitConstructor(null)
                                    .Parameters(FunctionParameter.Create("cp",
                                        NameFactory.ReferenceTypeReference(NameFactory.SelfTypeReference(MutabilityOverride.Neutral))))
                                    .SetModifier(EntityModifier.Pinned))
                                    );

            root_ns.AddBuilder(TypeBuilder.Create("Carbon")
                .Parents(NameReference.Create("IDuplicate"))
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Nat8TypeReference(), null, EntityModifier.Public))
                // default constructor
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("7")))))

                    // copy constructor (derived)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("7"))))
                    .SetModifier(EntityModifier.Pinned | EntityModifier.Override | EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("cp",
                        NameFactory.ReferenceTypeReference(NameFactory.SelfTypeReference(MutabilityOverride.Neutral)),
                            ExpressionReadMode.CannotBeRead)))

                            );

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference(NameReference.Create("IDuplicate")),
                        ExpressionFactory.HeapConstructor("Carbon")),

                    // we create a copy using `Self` type on external object 
                    VariableDeclaration.CreateStatement("b",null,
                        ExpressionFactory.HeapConstructor(NameReference.Create("a",NameFactory.SelfTypeTypeName),NameReference.Create("a"))),

                    // we should get our type back and the value we set in our copy constructor
                    IfBranch.CreateIf( ExpressionFactory.OptionalDeclaration("c",null,ExpressionFactory.DownCast(NameReference.Create("b"),
                        NameFactory.PointerTypeReference(NameReference.Create("Carbon")))),
                        Return.Create(NameReference.Create("c","x")),
                        IfBranch.CreateElse(Return.Create(Nat8Literal.Create("100"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)7, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter RuntimeSelfTypeResolution()
        {
            // the purpose of this test is to check whether `Self` is correctly resolved and appropriate constructor is called

            var env = Language.Environment.Create(new Options()
            {
                DiscardingAnyExpressionDuringTests = true,
                DebugThrowOnError = true
            });
            var root_ns = env.Root;


            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("IDuplicate"))

                                .With(FunctionBuilder.CreateInitConstructor(null)
                                    .Parameters(FunctionParameter.Create("cp",
                                        NameFactory.ReferenceTypeReference(NameFactory.SelfTypeReference(MutabilityOverride.Neutral))))
                                    .SetModifier(EntityModifier.Pinned))

                                    .With(FunctionBuilder.Create("copy",
                                        NameFactory.PointerTypeReference(NameReference.Create("IDuplicate")),
                                        Block.CreateStatement(Return.Create(ExpressionFactory.HeapConstructor(NameFactory.SelfTypeReference(),
                                            NameReference.CreateThised())))))
                                    );

            root_ns.AddBuilder(TypeBuilder.Create("Carbon")
                .Parents(NameReference.Create("IDuplicate"))
                .With(VariableDeclaration.CreateStatement("x",NameFactory.Nat8TypeReference(),null,EntityModifier.Public))
                // default constructor
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                    Assignment.CreateStatement(NameReference.CreateThised("x"),Nat8Literal.Create("7")))))

                    // copy constructor (derived)
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        Assignment.CreateStatement(NameReference.CreateThised("x"), Nat8Literal.Create("7"))))
                    .SetModifier(EntityModifier.Pinned | EntityModifier.Override | EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("cp",
                        NameFactory.ReferenceTypeReference(NameFactory.SelfTypeReference(MutabilityOverride.Neutral)),
                            ExpressionReadMode.CannotBeRead)))

                            );

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.StackConstructor("Carbon")),

                    // we call "copy" method which should in turn call copy constructor
                    VariableDeclaration.CreateStatement("b",null,
                        FunctionCall.Create(NameReference.Create("a","copy"))),

                    // we should get our type back and the value we set in our copy constructor
                    IfBranch.CreateIf( ExpressionFactory.OptionalDeclaration("c",null,ExpressionFactory.DownCast(NameReference.Create("b"),
                        NameFactory.PointerTypeReference(NameReference.Create("Carbon")))),
                        Return.Create(NameReference.Create("c","x")),
                        IfBranch.CreateElse(Return.Create(Nat8Literal.Create("100"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)7, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OptionalAssignment()
        {
            var env = Language.Environment.Create(new Options()
            {
                DebugThrowOnError = true,
                DiscardingAnyExpressionDuringTests = true,
            });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),


                    VariableDeclaration.CreateStatement("x", null,
                        ExpressionFactory.OptionOf(NameFactory.Nat8TypeReference(), Nat8Literal.Create("3"))),
                    VariableDeclaration.CreateStatement("y", null,
                        ExpressionFactory.OptionEmpty(NameFactory.Nat8TypeReference())),

                    VariableDeclaration.CreateStatement("a", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("b", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),

                    // succesful assignment
                    IfBranch.CreateIf(ExpressionFactory.OptionalAssignment(new[] { NameReference.Create("a") },
                        new[] { NameReference.Create("x") }),
                            ExpressionFactory.IncBy("acc", Nat8Literal.Create("2"))),
                    // failed assignment
                    IfBranch.CreateIf(ExpressionFactory.OptionalAssignment(new[] { NameReference.Create("b") },
                        new[] { NameReference.Create("y") }),
                            ExpressionFactory.IncBy("acc", Nat8Literal.Create("3"))),

                    Assignment.CreateStatement(NameReference.Create("a"), Nat8Literal.Create("0")),
                    // failed assignment (because the second of the nested assignments fails)
                    IfBranch.CreateIf(ExpressionFactory.OptionalAssignment(new[] { NameReference.Create("a"), NameReference.Create("b") },
                        new[] { NameReference.Create("x"), NameReference.Create("y") }),
                        ExpressionFactory.IncBy("acc", Nat8Literal.Create("7"))),
                    // checking that the first nested assignment was not executed (it is all-or-nothing)
                    ExpressionFactory.AssertEqual(Nat8Literal.Create("0"), NameReference.Create("a")),

                    ExpressionFactory.Readout("b"),

                    Return.Create(NameReference.Create("acc"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OverflowAddition()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("b",null,NameReference.Create(NameFactory.Nat64TypeReference(),
                        NameFactory.NumMaxValueName),EntityModifier.Reassignable),
                    Assignment.CreateStatement(NameReference.Create("b"),
                        ExpressionFactory.AddOverflow(NameReference.Create("b"),Nat64Literal.Create("3"))),
                    Return.Create(NameReference.Create("b"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2UL, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter PassingSelfTypeCheck()
        {
            var env = Language.Environment.Create(new Options()
            {
                AllowInvalidMainResult = true,
                DebugThrowOnError = true,
                DiscardingAnyExpressionDuringTests = true
            });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Tiny")
                .SetModifier(EntityModifier.Base)
                .Parents(NameFactory.IEquatableTypeReference())
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateFalse())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create(MutabilityOverride.Neutral, "Tiny"), ExpressionReadMode.CannotBeRead)))
                    );

            root_ns.AddBuilder(TypeBuilder.Create("Rich")
                .Parents("Tiny")
                .WithEquatableEquals(EntityModifier.UnchainBase)
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create(MutabilityOverride.Neutral, "Rich"), ExpressionReadMode.CannotBeRead)))
                    );


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    VariableDeclaration.CreateStatement("b",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("a",NameFactory.EqualOperator),
                        NameReference.Create("b")),Int64Literal.Create("2"),Int64Literal.Create("7")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DetectingImpostorSelfType()
        {
            var env = Language.Environment.Create(new Options()
            {
                DebugThrowOnError = true,
                AllowInvalidMainResult = true,
                DiscardingAnyExpressionDuringTests = true
            });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Tiny")
                .SetModifier(EntityModifier.Base)
                .Parents(NameFactory.IEquatableTypeReference())
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create(MutabilityOverride.Neutral, "Tiny"), ExpressionReadMode.CannotBeRead)))
                    );

            root_ns.AddBuilder(TypeBuilder.Create("Rich")
                .Parents("Tiny")
                .WithEquatableEquals(EntityModifier.UnchainBase)
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create(MutabilityOverride.Neutral, "Rich"), ExpressionReadMode.CannotBeRead)))
                    );


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Tiny")),
                    VariableDeclaration.CreateStatement("b",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    // since the call is dynamic, through IEquatable it should compile but throw in runtime because
                    // we have different types
                    ExpressionFactory.Readout(FunctionCall.Create(NameReference.Create("a",NameFactory.EqualOperator),
                        NameReference.Create("b"))),
                    Return.Create(Int64Literal.Create("44"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.IsTrue(result.IsThrow);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter TestingTypeInfo()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,Int64Literal.Create("1")),
                    VariableDeclaration.CreateStatement("b",null,Int64Literal.Create("2")),
                    VariableDeclaration.CreateStatement("c",null,RealLiteral.Create("1")),
                    VariableDeclaration.CreateStatement("x",null,
                        FunctionCall.Create(NameReference.Create("a",NameFactory.GetTypeFunctionName))),
                    VariableDeclaration.CreateStatement("y",null,
                        FunctionCall.Create(NameReference.Create("b",NameFactory.GetTypeFunctionName))),
                    VariableDeclaration.CreateStatement("z",null,
                        FunctionCall.Create(NameReference.Create("c",NameFactory.GetTypeFunctionName))),

                    VariableDeclaration.CreateStatement("acc", null, Int64Literal.Create("0"), EntityModifier.Reassignable),
                    IfBranch.CreateIf(IsSame.Create("x","y"),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),Int64Literal.Create("2")))
                    }),
                    IfBranch.CreateIf(IsSame.Create("x","z"),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),Int64Literal.Create("7")))
                    }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CorruptedParallelAssignmentWithSpread()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",NameFactory.Int64TypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",NameFactory.Int64TypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("z",NameFactory.Int64TypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.Tuple(Int64Literal.Create("-3"),Int64Literal.Create("5"))),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y"),NameReference.Create("z") },
                        new[]{  Spread.Create(NameReference.Create("a")) }),
                    ExpressionFactory.Readout("z"),
                    Return.Create(ExpressionFactory.Add("x","y"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.IsTrue(result.IsThrow);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ParallelAssignmentWithSpread()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",NameFactory.Int64TypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",NameFactory.Int64TypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.Tuple(Int64Literal.Create("-3"),Int64Literal.Create("5"))),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y") },
                        new[]{  Spread.Create(NameReference.Create("a")) }),
                    Return.Create(ExpressionFactory.Add("x","y"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ParallelAssignment()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",null,Int64Literal.Create("-5"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",null,Int64Literal.Create("2"), EntityModifier.Reassignable),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y") },
                        new[]{ NameReference.Create("y"),NameReference.Create("x") }),
                    Return.Create(NameReference.Create("x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AccessingObjectFields()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(VariableDeclaration.CreateStatement("y", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     Int64Literal.Create("2")),
                    Return.Create(NameReference.Create(NameReference.Create("p"),"x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter UsingEnums()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true, DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateEnum("Sizing")
                .With(EnumCaseBuilder.Create("small", "big")));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s",null,NameReference.Create("Sizing","small")),
                    IfBranch.CreateIf(ExpressionFactory.IsNotEqual(NameReference.Create( "s"),NameReference.Create("Sizing","big")),
                        new[]{ Return.Create(Int64Literal.Create("2")) }),
                    Return.Create(Int64Literal.Create("5"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ConstructorChaining()
        {
            var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            FunctionDefinition base_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                Block.CreateStatement(new[] {
                    // a = a + 5   --> 4
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName, "a"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"a"),Int64Literal.Create("5")))
                }));
            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(base_constructor)
                .With(VariableDeclaration.CreateStatement("a", NameFactory.Int64TypeReference(), Int64Literal.Create("-1"),
                    EntityModifier.Public | EntityModifier.Reassignable)));

            FunctionDefinition next_constructor = FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                Block.CreateStatement(new[] {
                    // b = b + 15 --> +5
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"b"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"b"),Int64Literal.Create("15")))
                }), ExpressionFactory.BaseInit());

            TypeDefinition next_type = root_ns.AddBuilder(TypeBuilder.Create("Next")
                .Parents("Point")
                .SetModifier(EntityModifier.Mutable | EntityModifier.Base)
                .With(next_constructor)

                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None,
                    new[] { FunctionParameter.Create("i", NameFactory.Int64TypeReference()) },
                    Block.CreateStatement(new[] {
                    // b = b + i  --> i+5
                    Assignment.CreateStatement(NameReference.Create(NameFactory.ThisVariableName,"b"),
                        ExpressionFactory.Add(NameReference.Create(NameFactory.ThisVariableName,"b"),NameReference.Create("i")))
                }), ExpressionFactory.ThisInit()))
                .With(VariableDeclaration.CreateStatement("b", NameFactory.Int64TypeReference(), Int64Literal.Create("-10"),
                    EntityModifier.Public | EntityModifier.Reassignable)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Next"),
                        FunctionArgument.Create(Int64Literal.Create("-7")))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("p","a"), NameReference.Create("p","b")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
