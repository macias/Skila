using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Expressions
    {
        [TestMethod]
        public IErrorReporter ErrorAddressingRValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            IntLiteral int_literal = IntLiteral.Create("1");

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDefiniton.CreateStatement("x", null, AddressOf.CreatePointer(int_literal)),
                    VariableDefiniton.CreateStatement("y", null, AddressOf.CreateReference(IntLiteral.Create("2"))),
                    Tools.Readout("x"),
                    Tools.Readout("y"),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AddressingRValue, int_literal));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ReadingIfAsExpression()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                new IExpression[] { IntLiteral.Create("5")
                },
                    IfBranch.CreateElse(new[] { IntLiteral.Create("7") }));
            var decl = VariableDefiniton.CreateStatement("x", null, if_ctrl);

            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingNonValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            NameReference non_value = NameFactory.IntTypeReference();
            var decl = VariableDefiniton.CreateStatement("x", null, initValue: non_value);

            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NoValueExpression, non_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringFunctionResult()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.ReadRequired,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    ExpressionFactory.Readout("x"),
                    Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDefiniton.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(Block.CreateStatement(new[] { call }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ExpressionValueNotUsed, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringParameters()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionParameter parameter = FunctionParameter.Create("x", NameFactory.IntTypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("foo"),
                new[] { parameter },
                ExpressionReadMode.ReadRequired,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, parameter));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningSimpleRValues()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getter"),
                null,
                ExpressionReadMode.ReadRequired,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("5")) })));

            FunctionCall call = FunctionCall.Create(NameReference.Create("getter"));
            NameReference func_ref = NameReference.Create("getter");
            root_ns.AddNode(Block.CreateStatement(new IExpression[] {
                VariableDefiniton.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()),
                // errors: assigning to r-value
                Assignment.CreateStatement(call,NameReference.Create("i")),
                Assignment.CreateStatement(func_ref,Undef.Create())
            }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, call));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, func_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningCompoundRValues()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDefiniton.CreateStatement("x", NameFactory.IntTypeReference(), null, EntityModifier.Reassignable)));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getter"),
                null,
                ExpressionReadMode.ReadRequired,
                NameReference.Create("Point"),
                Block.CreateStatement(new[] { Return.Create(Undef.Create()) })));

            NameReference field_ref = NameReference.Create(FunctionCall.Create(NameReference.Create("getter")), "x");
            root_ns.AddNode(Block.CreateStatement(new IExpression[] {
                // error: assigning to r-value
                Assignment.CreateStatement(field_ref,IntLiteral.Create("3")),
            }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingFunctionVoidResult()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.CannotBeRead,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] {
                    ExpressionFactory.Readout("x"),
                    Return.Create(BoolLiteral.CreateTrue()) })));

            root_ns.AddNode(VariableDefiniton.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            VariableDefiniton decl = VariableDefiniton.CreateStatement("x", NameFactory.BoolTypeReference(),
                call);
            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnusedExpression()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var var_decl = VariableDefiniton.CreateStatement("x", NameFactory.BoolTypeReference(), Undef.Create());
            var var_ref = NameReference.Create("x");
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                          NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                          ExpressionReadMode.OptionalUse,
                          NameFactory.BoolTypeReference(),
                          Block.CreateStatement(new IExpression[] {
                              var_decl,
                              var_ref,
                              Return.Create(BoolLiteral.CreateTrue())
                          })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ExpressionValueNotUsed, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(var_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorSelfAssignment()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var var_decl = VariableDefiniton.CreateStatement("x", NameFactory.BoolTypeReference(),
                Undef.Create(), EntityModifier.Reassignable);
            var assign = Assignment.CreateStatement(NameReference.Create("x"), NameReference.Create("x"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                          NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                          ExpressionReadMode.OptionalUse,
                          NameFactory.VoidTypeReference(),
                          Block.CreateStatement(new IExpression[] {
                              var_decl,
                              assign,
                          })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelfAssignment, assign));

            return resolver;
        }
    }
}