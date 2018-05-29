using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Templates
    {

        [TestMethod]
        public IInterpreter ResolvingGenericArgumentInRuntime()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("oracle", "O", VarianceMode.None,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    Return.Create(IsType.Create(NameReference.Create("thing"), NameReference.Create("O")))
                ))
                .Parameters(FunctionParameter.Create("thing", NameFactory.ReferenceTypeReference(NameFactory.IObjectTypeReference()))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("acc", null, Nat8Literal.Create("0"), EntityModifier.Reassignable),
                    VariableDeclaration.CreateStatement("i", null, ExpressionFactory.HeapConstructor(NameFactory.IntTypeReference(), IntLiteral.Create("7"))),
                    VariableDeclaration.CreateStatement("d", null, ExpressionFactory.HeapConstructor(NameFactory.RealTypeReference(), RealLiteral.Create("3.3"))),
                    IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("oracle", NameFactory.IntTypeReference()),
                        NameReference.Create("i")),
                        new[] { ExpressionFactory.IncBy("acc", Nat8Literal.Create("2")) }),
                    IfBranch.CreateIf(FunctionCall.Create(NameReference.Create("oracle", NameFactory.IntTypeReference()),
                        NameReference.Create("d")),
                        new[] { ExpressionFactory.IncBy("acc", Nat8Literal.Create("88")) }),
                    Return.Create(NameReference.Create("acc"))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual((byte)2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CheckingHostTraitRuntimeType()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));


            root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .SetModifier(EntityModifier.Trait)
                .Parents("ISay")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(
                        Return.Create(Int64Literal.Create("2"))
                    )).SetModifier(EntityModifier.Override)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    // just plain host, no trait is used
                    VariableDeclaration.CreateStatement("g", NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference()),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")))),
                    // we should have fail-test here
                    Return.Create(ExpressionFactory.Ternary(IsType.Create(NameReference.Create("g"), NameReference.Create("ISay")),
                        Int64Literal.Create("99"), Int64Literal.Create("2")))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CheckingTraitRuntimeType()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Say")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("7"))
                }))
                .SetModifier(EntityModifier.Override))
                .Parents("ISay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .SetModifier(EntityModifier.Trait)
                .Parents("ISay")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(
                        Return.Create(Int64Literal.Create("2"))
                    )).SetModifier(EntityModifier.Override)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("g", NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference()),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                    Return.Create(ExpressionFactory.Ternary(IsType.Create(NameReference.Create("g"), NameReference.Create("ISay")),
                        Int64Literal.Create("2"), Int64Literal.Create("88")))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingTraitMethodViaInterface()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Say")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("7"))
                }))
                .SetModifier(EntityModifier.Override))
                .Parents("ISay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .SetModifier(EntityModifier.Trait)
                .Parents("ISay")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(
                        Return.Create(Int64Literal.Create("2"))
                    )).SetModifier(EntityModifier.Override)));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    // crucial point, we store our object as interface *ISay
                    VariableDeclaration.CreateStatement("g", NameFactory.PointerTypeReference("ISay"),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                    // we call method "say" implemented in trait
                    Return.Create(FunctionCall.Create(NameReference.Create("g", "say")))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter CallingTraitMethod()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true, AllowInvalidMainResult = true }.DisableSingleMutability());
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Say")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(Int64Literal.Create("2"))
                }))
                .SetModifier(EntityModifier.Override))
                .Parents("ISay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .SetModifier(EntityModifier.Trait)
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .With(FunctionBuilder.Create("hello", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    Return.Create(FunctionCall.Create(NameReference.Create("s", "say")))
                ))
                .Parameters(FunctionParameter.Create("s", NameFactory.ReferenceTypeReference("X")))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("g", null,
                        ExpressionFactory.StackConstructor(NameReference.Create("Greeter", NameReference.Create("Say")))),
                    VariableDeclaration.CreateStatement("y", null, ExpressionFactory.StackConstructor("Say")),
                    Return.Create(FunctionCall.Create(NameReference.Create("g", "hello"), NameReference.Create("y")))
                )));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithPointer()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true, AllowProtocols = true, DebugThrowOnError = true }.DisableSingleMutability());
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                     ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameFactory.PointerTypeReference("T"), Variadic.None, null, isNameRequired: false)));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create("getMe",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.HeapConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter HasConstraintWithValue()
        {
            var env = Environment.Create(new Options()
            {
                AllowInvalidMainResult = true,
                AllowProtocols = true,
                DebugThrowOnError = true
            }.DisableSingleMutability());
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameReference.Create("T"))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Y")
                .With(FunctionBuilder.Create("getMe",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y",null,ExpressionFactory.StackConstructor(NameReference.Create("Y"))),
                    Return.Create(call)
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}