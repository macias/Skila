﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Builders;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Variables
    {
        [TestMethod]
        public IErrorReporter DetectingUsage()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    Assignment.CreateStatement(NameReference.Sink(),
                        VariableDeclaration.CreateExpression("result", NameFactory.IntTypeReference(),initValue: Undef.Create())),
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReassigningFixedVariable()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            Assignment assignment = Assignment.CreateStatement(NameReference.Create("x"), IntLiteral.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", null, IntLiteral.Create("3")),
                    assignment,
                    Tools.Readout("x") })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, assignment));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorCompoundDefaultValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var decl = root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameReferenceUnion.Create(
                new[] { NameFactory.PointerTypeReference(NameFactory.BoolTypeReference()),
                    NameFactory.PointerTypeReference(NameFactory.IntTypeReference())}),
                initValue: null));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotAutoInitializeCompoundType, decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter VariableBinding()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var decl_1 = VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create());
            root_ns.AddNode(decl_1);
            var var_1_ref = NameReference.Create("x");
            var decl_2 = root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(),
                var_1_ref));

            var resolver = NameResolver.Create(env);
            Assert.AreEqual(1, decl_1.TypeName.Binding().Matches.Count);
            Assert.AreEqual(env.DoubleType, decl_1.TypeName.Binding().Match.Target);

            Assert.AreEqual(1, var_1_ref.Binding.Matches.Count);
            Assert.AreEqual(decl_1, var_1_ref.Binding.Match.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AssignmentTypeChecking()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create()));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(), NameReference.Create("x")));
            var x_ref = NameReference.Create("x");
            root_ns.AddNode(VariableDeclaration.CreateStatement("z", NameFactory.IntTypeReference(), x_ref));

            var resolver = NameResolver.Create(env);
            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(x_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TypeInference()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var var_x = VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create());
            var var_y = VariableDeclaration.CreateStatement("y", null, NameReference.Create("x"));
            var var_z = VariableDeclaration.CreateStatement("z", null, null);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { var_x, var_y, var_z, Tools.Readout("z"), Tools.Readout("y") })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.MissingTypeAndValue, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(var_z, resolver.ErrorManager.Errors.Single().Node);

            Assert.AreEqual(env.DoubleType, var_y.Evaluation.Target());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctionAssignment()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var func_def = root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("t", NameFactory.IntTypeReference(), Variadic.None,
                    null,isNameRequired: false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x",
                NameReference.Create("Function", NameFactory.IntTypeReference(), NameFactory.DoubleTypeReference()),
                initValue: NameReference.Create("foo")));
            var foo_ref = NameReference.Create("foo");
            root_ns.AddNode(VariableDeclaration.CreateStatement("y",
                NameReference.Create("Function", NameFactory.DoubleTypeReference(), NameFactory.IntTypeReference()),
                initValue: foo_ref));

            var resolver = NameResolver.Create(env);
            Assert.AreEqual(1, foo_ref.Binding.Matches.Count);
            Assert.AreEqual(func_def, foo_ref.Binding.Match.Target);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(foo_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorVariableNotUsed()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var decl1 = VariableDeclaration.CreateStatement("s", NameFactory.IntTypeReference(), null);
            var decl2 = VariableDeclaration.CreateStatement("t", NameFactory.IntTypeReference(), null);
            var loop = Loop.CreateFor(NameDefinition.Create("here"),
              init: null,
              preCheck: null,
              step: null,
              body: new IExpression[] { decl1, decl2 });
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    loop,
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl2));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, loop));

            return resolver;
        }
    }
}