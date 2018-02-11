using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public class Properties
    {
        [TestMethod]
        public IInterpreter OverridingMethodWithIndexerGetter()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IProvider")
                .With(FunctionBuilder.CreateDeclaration(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference()))));

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .Parents("IProvider")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create(NameFactory.PropertyIndexerName, ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(Return.Create(Int64Literal.Create("500"))))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase)
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead))));

            root_ns.AddBuilder(TypeBuilder.Create("Last")
                .Parents("Middle")
                .Modifier(EntityModifier.Base)
                .With(PropertyBuilder.CreateIndexer(NameFactory.Int64TypeReference())
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead))
                    .With(PropertyMemberBuilder.CreateIndexerGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                        .Modifier(EntityModifier.Override | EntityModifier.UnchainBase))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerTypeReference("IProvider"),
                        ExpressionFactory.HeapConstructor("Last")),
                    Return.Create(FunctionCall.Create(NameReference.Create("p",NameFactory.PropertyIndexerName),
                        FunctionArgument.Create(Int64Literal.Create("18"))))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter OverridingMethodWithGetter()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IProvider")
                .With(FunctionBuilder.CreateDeclaration("getMe", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .Parents("IProvider")
                .Modifier(EntityModifier.Base)
                .With(FunctionBuilder.Create("getMe", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(Return.Create(Int64Literal.Create("500"))))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase)));

            root_ns.AddBuilder(TypeBuilder.Create("Last")
                .Parents("Middle")
                .Modifier(EntityModifier.Base)
                .With(PropertyBuilder.Create("getMe", NameFactory.Int64TypeReference())
                    .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                        .Modifier(EntityModifier.Override | EntityModifier.UnchainBase))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",NameFactory.PointerTypeReference("IProvider"),
                        ExpressionFactory.HeapConstructor("Last")),
                    Return.Create(FunctionCall.Create( NameReference.Create("p","getMe")))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter Indexer()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            IEnumerable<FunctionParameter> property_parameters = new[] { FunctionParameter.Create("idx", NameFactory.Int64TypeReference()) };
            NameReference property_typename = NameFactory.Int64TypeReference();

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.CreateIndexer(property_typename,
                    new[] { VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), Int64Literal.Create("1"),
                        EntityModifier.Reassignable) },
                    new[] { Property.CreateIndexerGetter(property_typename, property_parameters,
                        Block.CreateStatement(IfBranch.CreateIf(ExpressionFactory.IsEqual(NameReference.Create("idx"),Int64Literal.Create("17")),new[]{
                            Return.Create(NameReference.CreateThised("x"))
                            },IfBranch.CreateElse(new[]{
                                Return.Create(Int64Literal.Create("300"))
                            })))) },
                    new[] { Property.CreateIndexerSetter(property_typename, property_parameters,
                        Block.CreateStatement(IfBranch.CreateIf(ExpressionFactory.IsEqual(NameReference.Create("idx"),Int64Literal.Create("17")),new[]{
                            Assignment.CreateStatement(NameReference.CreateThised("x"),
                                NameReference.Create(NameFactory.PropertySetterValueParameter))
                            }))) }
                )));

            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
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

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }

        [TestMethod]
        public IInterpreter AutoProperties()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.Int64TypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.Int64TypeReference(), Int64Literal.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.Int64TypeReference()) },
                    new[] { Property.CreateAutoSetter(NameFactory.Int64TypeReference()) }
                )));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    // p = Point() // p.x is initialized with 1
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor(NameReference.Create("Point"))),
                    // p.x = 1+p.x
                    Assignment.CreateStatement(NameReference.Create(NameReference.Create("p"),"x"),
                     FunctionCall.Create(NameReference.Create( Int64Literal.Create("1"), NameFactory.AddOperator),
                     FunctionArgument.Create(NameReference.Create("p","x")))),
                    // return p.x
                    Return.Create(NameReference.Create("p","x"))
                })));

            var interpreter = new Interpreter.Interpreter();
            ExecValue result = interpreter.TestRun(env);

            Assert.AreEqual(2L, result.RetValue.PlainValue);

            return interpreter;
        }
    }
}
