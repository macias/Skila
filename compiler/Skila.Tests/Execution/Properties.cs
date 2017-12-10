﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using System.Collections.Generic;

namespace Skila.Tests.Execution
{
    [TestClass]
    public class Properties
    {
        [TestMethod]
        public IInterpreter Indexer()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            IEnumerable<FunctionParameter> property_parameters = new[] { FunctionParameter.Create("idx", NameFactory.IntTypeReference()) };
            NameReference property_typename = NameFactory.IntTypeReference();

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.CreateIndexer(property_typename,
                    new[] { VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("1"),
                        EntityModifier.Reassignable) },
                    new[] { Property.CreateIndexerGetter(property_typename, property_parameters,
                        IfBranch.CreateIf(ExpressionFactory.Equal(NameReference.Create("idx"),IntLiteral.Create("17")),new[]{
                            Return.Create(NameReference.CreateThised("x"))
                            },IfBranch.CreateElse(new[]{
                                Return.Create(IntLiteral.Create("300"))
                            }))) },
                    new[] { Property.CreateIndexerSetter(property_typename, property_parameters,
                        IfBranch.CreateIf(ExpressionFactory.Equal(NameReference.Create("idx"),IntLiteral.Create("17")),new[]{
                            Assignment.CreateStatement(NameReference.CreateThised("x"),
                                NameReference.Create(NameFactory.PropertySetterValueParameter))
                            })) }
                )));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p[17] = 1+p[17]
                    Assignment.CreateStatement(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(IntLiteral.Create("17"))),
                     FunctionCall.Create(NameReference.Create( IntLiteral.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(IntLiteral.Create("17")))))),
                    // return p[17]
                    Return.Create(FunctionCall.Indexer(NameReference.Create("p"),
                        FunctionArgument.Create(IntLiteral.Create("17"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AutoProperties()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.IntTypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.IntTypeReference(), IntLiteral.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.IntTypeReference()) },
                    new[] { Property.CreateAutoSetter(NameFactory.IntTypeReference()) }
                )));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p.x = 1+p.x
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     FunctionCall.Create(NameReference.Create( IntLiteral.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(NameReference.Create("p","x")))),
                    // return p.x
                    Return.Create(NameReference.Create("p","x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
