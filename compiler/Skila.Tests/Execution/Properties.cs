﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using System.Collections.Generic;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Properties : ITest
    {
        [TestMethod]
        public IInterpreter OverridingMethodWithIndexerGetter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IProvider")
                    .With(FunctionBuilder.CreateDeclaration(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference()))));

                root_ns.AddBuilder(TypeBuilder.Create("Middle")
                    .Parents("IProvider")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(Return.Create(Int64Literal.Create("500"))))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase)
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead))));

                root_ns.AddBuilder(TypeBuilder.Create("Last")
                    .Parents("Middle")
                    .SetModifier(EntityModifier.Base)
                    .With(PropertyBuilder.CreateIndexer(env.Options, NameFactory.Int64NameReference())
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), ExpressionReadMode.CannotBeRead))
                        .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                            .Modifier(EntityModifier.Override | EntityModifier.UnchainBase))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerNameReference("IProvider"),
                         ExpressionFactory.HeapConstructor("Last")),
                    Return.Create(FunctionCall.Create(NameReference.Create("p",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(Int64Literal.Create("18"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OverridingMethodWithGetter()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IProvider")
                    .With(FunctionBuilder.CreateDeclaration("getMe", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("Middle")
                    .Parents("IProvider")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create("getMe", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(Return.Create(Int64Literal.Create("500"))))
                        .SetModifier(EntityModifier.Override | EntityModifier.UnchainBase)));

                root_ns.AddBuilder(TypeBuilder.Create("Last")
                    .Parents("Middle")
                    .SetModifier(EntityModifier.Base)
                    .With(PropertyBuilder.Create(env.Options, "getMe", ()=>NameFactory.Int64NameReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                            .Modifier(EntityModifier.Override | EntityModifier.UnchainBase))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerNameReference("IProvider"),
                         ExpressionFactory.HeapConstructor("Last")),
                    Return.Create(FunctionCall.Create( NameReference.Create("p","getMe")))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter Indexer()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                IEnumerable<FunctionParameter> property_parameters = new[] { FunctionParameter.Create("idx", NameFactory.Int64NameReference()) };
                NameReference property_typename = NameFactory.Int64NameReference();

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(Property.CreateIndexer(env.Options, property_typename,
                        new[] { VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), Int64Literal.Create("1"),
                            env.Options.ReassignableModifier()) },
                        new[] { Property.CreateIndexerGetter(property_typename, property_parameters,
                        Block.CreateStatement(IfBranch.CreateIf( ExpressionFactory.IsEqual(NameReference.Create("idx"),Int64Literal.Create("17")),new[]{
                            Return.Create(NameReference.CreateThised("x"))
                            },IfBranch.CreateElse(new[]{
                                Return.Create(Int64Literal.Create("300"))
                            })))) },
                        new[] { Property.CreateIndexerSetter(property_typename, property_parameters,
                        Block.CreateStatement(IfBranch.CreateIf( ExpressionFactory.IsEqual(NameReference.Create("idx"),Int64Literal.Create("17")),new[]{
                            Assignment.CreateStatement(NameReference.CreateThised("x"),
                                NameReference.Create(NameFactory.PropertySetterValueParameter))
                            }))) }
                    )));

                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null, ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p[17] = 1+p[17]
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(Int64Literal.Create("17"))),
                     FunctionCall.Create(NameReference.Create( Int64Literal.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(Int64Literal.Create("17")))))),
                    // return p[17]
                    Return.Create(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(Int64Literal.Create("17"))))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AutoProperties()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true,
                    DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(Property.Create(env.Options, "x", NameFactory.Int64NameReference(),
                        new[] { Property.CreateAutoField(NameFactory.PropertyAutoField,NameFactory.Int64NameReference(), Int64Literal.Create("1"), 
                            env.Options.ReassignableModifier()) },
                        new[] { Property.CreateAutoGetter(NameFactory.PropertyAutoField, NameFactory.Int64NameReference()) },
                        new[] { Property.CreateAutoSetter(NameFactory.PropertyAutoField, NameFactory.Int64NameReference()) }
                    )));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null, ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p.x = 1+p.x
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     FunctionCall.Create(NameReference.Create( Int64Literal.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(NameReference.Create("p","x")))),
                    // return p.x
                    Return.Create(NameReference.Create("p","x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual(2L, result.RetValue.PlainValue);
            }

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AutoPropertiesWithPointers()
        {
            var interpreter = new Interpreter.Interpreter();

            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(Property.Create(env.Options, "x", NameFactory.PointerNameReference(NameFactory.Nat8NameReference()),
                        new[] { Property.CreateAutoField(NameFactory.PropertyAutoField,NameFactory.PointerNameReference(NameFactory.Nat8NameReference()),
                         ExpressionFactory.HeapConstructor(NameFactory.Nat8NameReference(),  Nat8Literal.Create("1")),
                            env.Options.ReassignableModifier()) },
                        new[] { Property.CreateAutoGetter(NameFactory.PropertyAutoField, NameFactory.PointerNameReference(NameFactory.Nat8NameReference())) },
                        new[] { Property.CreateAutoSetter(NameFactory.PropertyAutoField, NameFactory.PointerNameReference(NameFactory.Nat8NameReference())) }
                    )));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null, ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p.x = 1+p.x
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     ExpressionFactory.HeapConstructor(NameFactory.Nat8NameReference(),
                     FunctionCall.Create(NameReference.Create( Nat8Literal.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(NameReference.Create("p","x"))))),
                    // return p.x
                    Return.Create(NameReference.Create("p","x"))
                    })));

                ExecValue result = interpreter.TestRun(env);

                Assert.AreEqual((byte)2, result.RetValue.PlainValue);
            }

            return interpreter;
        }
    }
}
