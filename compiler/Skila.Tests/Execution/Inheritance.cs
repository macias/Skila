using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Inheritance
    {
        [TestMethod]
        public IInterpreter InheritingEnums()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateEnum("Weekend")
                    .With(EnumCaseBuilder.Create("Sat", "Sun"))
                    .SetModifier(EntityModifier.Base));

                root_ns.AddBuilder(TypeBuilder.CreateEnum("First")
                    .With(EnumCaseBuilder.Create("Mon"))
                    .Parents("Weekend"));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.NatNameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // let a Weekend = First.Sat
                    VariableDeclaration.CreateStatement("a",NameReference.Create("Weekend"),  
                       // please note we only refer to "Sat" through "First", the type is still "Weekend"
                        NameReference.Create("First","Sat")),
                    // var b First = Weekend.Sun
                    VariableDeclaration.CreateStatement("b",NameReference.Create("First"), NameReference.Create("Weekend","Sun"),
                        env.Options.ReassignableModifier()),
                    // b = First.Mon
                    Assignment.CreateStatement(NameReference.Create("b"),NameReference.Create("First","Mon")),
                    // let x = a to Nat; // 0
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.ConvCall(NameReference.Create("a"),
                        NameFactory.NatNameReference())),
                    // let y = b to Nat; // 2
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.ConvCall(NameReference.Create("b"),
                        NameFactory.NatNameReference())),
                    // return x + y
                    Return.Create(ExpressionFactory.Add(NameReference.Create("x"),NameReference.Create("y")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2UL, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter TypeUnion()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    AllowProtocols = true,
                    AllowInvalidMainResult = true,
                    DebugThrowOnError = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("GetPos")
                    .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("3"))
                        }))));

                root_ns.AddBuilder(TypeBuilder.Create("GetNeg")
                    .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("-1"))
                        }))));

                NameReferenceUnion union = NameReferenceUnion.Create(NameFactory.PointerNameReference(NameReference.Create("GetNeg")),
                    NameFactory.PointerNameReference(NameReference.Create("GetPos")));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",union, Undef.Create(),env.Options.ReassignableModifier()),
                    VariableDeclaration.CreateStatement("b",union, Undef.Create(),env.Options.ReassignableModifier()),
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructor("GetPos")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructor("GetNeg")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getSome"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("x"),NameReference.Create("y")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter TypeIntersection()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true, AllowProtocols = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetPos")
                    .With(FunctionBuilder.CreateDeclaration("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetNeg")
                    .With(FunctionBuilder.CreateDeclaration("getMore", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("GetAll")
                    .Parents("IGetPos", "IGetNeg")
                    .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("3"))
                        }))
                        .SetModifier(EntityModifier.Override)
                        )
                    .With(FunctionBuilder.Create("getMore", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("-1"))
                        }))
                        .SetModifier(EntityModifier.Override)
                        ));

                NameReferenceIntersection intersection = NameReferenceIntersection.Create(
                    NameFactory.PointerNameReference(NameReference.Create("IGetNeg")),
                    NameFactory.PointerNameReference(NameReference.Create("IGetPos")));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",intersection, Undef.Create(),env.Options.ReassignableModifier()),
                    VariableDeclaration.CreateStatement("b",intersection, Undef.Create(),env.Options.ReassignableModifier()),
                    Assignment.CreateStatement(NameReference.Create("a"),ExpressionFactory.HeapConstructor("GetAll")),
                    Assignment.CreateStatement(NameReference.Create("b"),ExpressionFactory.HeapConstructor("GetAll")),
                    VariableDeclaration.CreateStatement("x",null, FunctionCall.Create(NameReference.Create("a","getSome"))),
                    VariableDeclaration.CreateStatement("y",null, FunctionCall.Create(NameReference.Create("b","getMore"))),
                    Return.Create(ExpressionFactory.Add(NameReference.Create("x"),NameReference.Create("y")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter VirtualCall()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("MyBase")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(
                        "bar",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("33"))
                        }))
                        .SetModifier(EntityModifier.Base)));

                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("SomeChild")
                    .With(FunctionBuilder.Create("bar",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase))
                    .Parents(NameReference.Create("MyBase")));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerNameReference(NameReference.Create("MyBase")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("SomeChild"))),
                    Return.Create(FunctionCall.Create(NameReference.Create("i","bar")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter VirtualCallAtBase()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { ReferencingBase = true, AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IBase")
                    .With(FunctionBuilder.CreateDeclaration("getA", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference()))
                    .With(FunctionBuilder.CreateDeclaration("getB", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("Middle")
                    .Parents("IBase")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(
                        "getA",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("-50"))
                        }))
                        .SetModifier(EntityModifier.Override))
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("51"))
                        }))
                        .SetModifier(EntityModifier.Override)));

                root_ns.AddBuilder(TypeBuilder.Create("End")
                    .Parents("Middle")
                    .With(FunctionBuilder.Create(
                        "getA",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("-1000"))
                        }))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase))
                    .With(FunctionBuilder.Create(
                        "getB",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        // return 1+super()+base.getA()
                        Return.Create(ExpressionFactory.Add( Int64Literal.Create("1"),
                            ExpressionFactory.Add(FunctionCall.Create(NameReference.Create(NameFactory.SuperFunctionName)),
                                FunctionCall.Create(NameReference.Create(NameFactory.BaseVariableName,"getA")))))
                        }))
                        .SetModifier(EntityModifier.Override)));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // i *IBase
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerNameReference(NameReference.Create("IBase")),
                        null,env.Options.ReassignableModifier()),
                    // i = new End()
                    Assignment.CreateStatement(NameReference.Create("i"),
                        ExpressionFactory.HeapConstructor(NameReference.Create("End"))),
                    // return i.getB()
                    Return.Create(FunctionCall.Create(NameReference.Create("i","getB")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}