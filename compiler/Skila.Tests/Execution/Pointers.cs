using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using System;
using System.Linq;
using Skila.Interpreter;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Pointers
    {
        [TestMethod]
        public void PointerArgumentAutoDereference()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("inc"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Return.Create(ExpressionFactory.AddOperator(NameReference.Create("n"),IntLiteral.Create("1")))
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.IntTypeReference(), Variadic.None, null, isNameRequired: false)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(),FunctionArgument.Create(IntLiteral.Create("1")))),
                    Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( NameReference.Create("p_int")))),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DereferenceOnReturn()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(),FunctionArgument.Create(IntLiteral.Create("2")))),
                    Return.Create( NameReference.Create("p_int")),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DereferenceOnIfCondition()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("ptr",NameFactory.PointerTypeReference(NameFactory.BoolTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.BoolTypeReference(),FunctionArgument.Create(BoolLiteral.CreateTrue()))),
                    Return.Create( IfBranch.CreateIf( NameReference.Create("ptr"),new[]{ IntLiteral.Create("2") },
                        IfBranch.CreateElse(new[]{ IntLiteral.Create("5") }))),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DiscardedReadout()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(),FunctionArgument.Create(IntLiteral.Create("2")))),
                    Tools.Readout("p_int"),
                    Return.Create( NameReference.Create("p_int")),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DereferenceOnAssignment()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(),FunctionArgument.Create(IntLiteral.Create("1")))),
                    VariableDeclaration.CreateStatement("v_int",NameFactory.IntTypeReference(), NameReference.Create("p_int")),
                    VariableDeclaration.CreateStatement("z_int",NameFactory.IntTypeReference(), null,EntityModifier.Reassignable),
                    Assignment.CreateStatement(NameReference.Create( "z_int"), NameReference.Create("p_int")),
                    Return.Create( FunctionCall.Create(NameReference.Create( NameReference.Create("v_int"),NameFactory.AddOperator),
                        FunctionArgument.Create(NameReference.Create("z_int"))))
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void DirectRefCountings()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var inc_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("inc"),
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("temp",NameFactory.PointerTypeReference( NameFactory.IntTypeReference()),NameReference.Create("n")),
                    Return.Create(FunctionCall.Create(NameReference.Create( NameReference.Create("temp"),NameFactory.AddOperator),
                    FunctionArgument.Create(IntLiteral.Create("1"))))
                }))
                .Parameters(FunctionParameter.Create("n", NameFactory.PointerTypeReference(NameFactory.IntTypeReference()), Variadic.None, null, isNameRequired: false)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p_int",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                        ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(),FunctionArgument.Create(IntLiteral.Create("1")))),
                    VariableDeclaration.CreateStatement("proxy",NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),NameReference.Create("p_int")),
                    Return.Create( FunctionCall.Create(NameReference.Create("inc"),FunctionArgument.Create( NameReference.Create("proxy")))),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }

        [TestMethod]
        public void NestedRefCountings()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var chain_type = root_ns.AddBuilder(TypeBuilder.Create("Chain")
                .With(VariableDeclaration.CreateStatement("v", NameFactory.IntTypeReference(), null, EntityModifier.Reassignable))
                .With(VariableDeclaration.CreateStatement("n", NameFactory.PointerTypeReference(NameReference.Create("Chain")),
                    Undef.Create(), EntityModifier.Reassignable)));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerTypeReference(NameReference.Create("Chain")),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Chain"))),
                    Assignment.CreateStatement(NameReference.Create("p","n"),
                        ExpressionFactory.HeapConstructorCall(NameReference.Create("Chain"))),
                    Assignment.CreateStatement(NameReference.Create("p","n","v"),IntLiteral.Create("2")),
                    Return.Create(NameReference.Create("p","n","v")),
                })));


            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);
        }
    }
}
