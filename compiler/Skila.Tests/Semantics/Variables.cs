using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Variables
    {
        [TestMethod]
        public IErrorReporter ErrorInvalidVariable()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            VariableDeclaration decl = VariableDeclaration.CreateStatement("x", null, IntLiteral.Create("3"), modifier: EntityModifier.Public);
            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalVariable, decl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingTypeName, decl));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter DetectingUsage()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("anything"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
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
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            IExpression assignment = Assignment.CreateStatement(NameReference.Create("x"), IntLiteral.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", null, IntLiteral.Create("3")),
                    assignment,
                    ExpressionFactory.Readout("x") })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, assignment));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorCompoundDefaultValue()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var decl = root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameReferenceUnion.Create(
                new[] { NameFactory.PointerTypeReference(NameFactory.BoolTypeReference()),
                    NameFactory.PointerTypeReference(NameFactory.IntTypeReference())}),
                initValue: null, modifier: EntityModifier.Public));

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

            var decl_1 = VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create(), modifier: EntityModifier.Public);
            root_ns.AddNode(decl_1);
            var var_1_ref = NameReference.Create("x");
            var decl_2 = root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(),
                var_1_ref, modifier: EntityModifier.Public));

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
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create()));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(), NameReference.Create("x"),
                modifier: EntityModifier.Public));
            var x_ref = NameReference.Create("x");
            root_ns.AddNode(VariableDeclaration.CreateStatement("z", NameFactory.IntTypeReference(), x_ref,
                modifier: EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, x_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TypeInference()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var var_x = VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create());
            var var_y = VariableDeclaration.CreateStatement("y", null, NameReference.Create("x"));
            var var_z = VariableDeclaration.CreateStatement("z", null, null);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    var_x,
                    var_y,
                    var_z,
                    ExpressionFactory.Readout("z"),
                    ExpressionFactory.Readout("y")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingTypeAndValue, var_z));

            Assert.AreEqual(env.DoubleType.InstanceOf, var_y.Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctionAssignment()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("t", NameFactory.IntTypeReference(), Variadic.None,
                    null,isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3"))
                })));

            // x *IFunction<Int,Double> = foo
            root_ns.AddNode(VariableDeclaration.CreateStatement("x",
                NameFactory.PointerTypeReference(NameReference.Create(NameFactory.IFunctionTypeName, NameFactory.IntTypeReference(), NameFactory.DoubleTypeReference())),
                initValue: NameReference.Create("foo"), modifier: EntityModifier.Public));
            var foo_ref = NameReference.Create("foo");
            // y *IFunction<Double,Int> = foo
            VariableDeclaration decl = VariableDeclaration.CreateStatement("y",
                NameFactory.PointerTypeReference(NameReference.Create(NameFactory.IFunctionTypeName, NameFactory.DoubleTypeReference(), NameFactory.IntTypeReference())),
                initValue: foo_ref, modifier: EntityModifier.Public);
            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, foo_ref.Binding.Matches.Count);
            Assert.AreEqual(func_def, foo_ref.Binding.Match.Target);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, decl.InitValue));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorVariableNotUsed()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            VariableDeclaration member = VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(),
                IntLiteral.Create("5"));
            root_ns.AddBuilder(TypeBuilder.Create("Thing")
                .With(member));

            var decl1 = VariableDeclaration.CreateStatement("s", NameFactory.IntTypeReference(), null);
            var decl2 = VariableDeclaration.CreateStatement("t", NameFactory.IntTypeReference(), null);
            var loop = Loop.CreateFor(NameDefinition.Create("here"),
              init: null,
              condition: null,
              step: null,
              body: new IExpression[] { decl1, decl2 });
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    loop,
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl2));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, loop));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, member));

            return resolver;
        }
    }
}