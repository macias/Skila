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
        public IInterpreter PassingSelfTypeCheck()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Tiny")
                .Modifier(EntityModifier.Base)
                .Parents(NameFactory.IEquatableTypeReference())
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateFalse())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create("Tiny"), ExpressionReadMode.CannotBeRead)))
                    );

            root_ns.AddBuilder(TypeBuilder.Create("Rich")
                .Parents("Tiny")
                .WithEquatableEquals(EntityModifier.UnchainBase)
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create("Rich"), ExpressionReadMode.CannotBeRead)))
                    );


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    VariableDeclaration.CreateStatement("b",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    Return.Create(ExpressionFactory.Ternary(FunctionCall.Create(NameReference.Create("a",NameFactory.EqualOperator),
                        NameReference.Create("b")),IntLiteral.Create("2"),IntLiteral.Create("7")))                    
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter DetectingImpostorSelfType()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Tiny")
                .Modifier(EntityModifier.Base)
                .Parents(NameFactory.IEquatableTypeReference())
                .WithEquatableEquals()
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create("Tiny"), ExpressionReadMode.CannotBeRead)))
                    );

            root_ns.AddBuilder(TypeBuilder.Create("Rich")
                .Parents("Tiny")
                .WithEquatableEquals(EntityModifier.UnchainBase)
                .With(FunctionBuilder.Create(NameFactory.EqualOperator,
                    NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateTrue())))
                    .Parameters(FunctionParameter.Create("cmp", NameReference.Create("Rich"), ExpressionReadMode.CannotBeRead)))
                    );


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Tiny")),
                    VariableDeclaration.CreateStatement("b",NameFactory.PointerTypeReference(NameFactory.IEquatableTypeReference()),
                        ExpressionFactory.HeapConstructor("Rich")),
                    ExpressionFactory.Readout(FunctionCall.Create(NameReference.Create("a",NameFactory.EqualOperator),
                        NameReference.Create("b"))),
                    Return.Create(IntLiteral.Create("44"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(DataMode.Throw, result.Mode);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter TestingTypeInfo()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",null,IntLiteral.Create("1")),
                    VariableDeclaration.CreateStatement("b",null,IntLiteral.Create("2")),
                    VariableDeclaration.CreateStatement("c",null,DoubleLiteral.Create("1")),
                    VariableDeclaration.CreateStatement("x",null,
                        FunctionCall.Create(NameReference.Create("a",NameFactory.GetTypeFunctionName))),
                    VariableDeclaration.CreateStatement("y",null,
                        FunctionCall.Create(NameReference.Create("b",NameFactory.GetTypeFunctionName))),
                    VariableDeclaration.CreateStatement("z",null,
                        FunctionCall.Create(NameReference.Create("c",NameFactory.GetTypeFunctionName))),

                    VariableDeclaration.CreateStatement("acc", null, IntLiteral.Create("0"), EntityModifier.Reassignable),
                    IfBranch.CreateIf(IsSame.Create("x","y"),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),IntLiteral.Create("2")))
                    }),
                    IfBranch.CreateIf(IsSame.Create("x","z"),new[]{
                        Assignment.CreateStatement(NameReference.Create("acc"),
                            ExpressionFactory.Add(NameReference.Create("acc"),IntLiteral.Create("7")))
                    }),
                    Return.Create(NameReference.Create("acc"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CorruptedParallelAssignmentWithSpread()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true, DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",NameFactory.IntTypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",NameFactory.IntTypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("z",NameFactory.IntTypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.Tuple(IntLiteral.Create("-3"),IntLiteral.Create("5"))),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y"),NameReference.Create("z") },
                        new[]{  Spread.Create(NameReference.Create("a")) }),
                    ExpressionFactory.Readout("z"),
                    Return.Create(ExpressionFactory.Add("x","y"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(DataMode.Throw, result.Mode);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ParallelAssignmentWithSpread()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;


            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("x",NameFactory.IntTypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("y",NameFactory.IntTypeReference(),null, EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("a",null,ExpressionFactory.Tuple(IntLiteral.Create("-3"),IntLiteral.Create("5"))),
                    Assignment.CreateStatement(new[]{ NameReference.Create("x"), NameReference.Create("y") },
                        new[]{  Spread.Create(NameReference.Create("a")) }),
                    Return.Create(ExpressionFactory.Add("x","y"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter ParallelAssignment()
        {
            var env = Language.Environment.Create(new Options() { DebugThrowOnError = true });
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
