using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;
using Skila.Language.Comparers;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Variables
    {
        [TestMethod]
        public IErrorReporter ErrorIfScope()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference bad_ref = NameReference.Create("x");

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "testing",
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("b", NameFactory.BoolNameReference(), Undef.Create(),
                            env.Options.ReassignableModifier()),

                        IfBranch.CreateIf(VariableDeclaration.CreateExpression("x", null, BoolLiteral.CreateTrue()),
                            // x is in scope
                            Assignment.CreateStatement("b", "x"),
                            IfBranch.CreateElse(
                                // x is in scope as well
                                Assignment.CreateStatement("b", "x"))),

                        // here x is not in the scope (is already removed)
                        Assignment.CreateStatement(NameReference.Create("b"), bad_ref),
                        ExpressionFactory.Readout("b")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, bad_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInvalidVariable()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl = VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("3"), modifier: EntityModifier.Public);
                root_ns.AddNode(decl);

                VariableDeclaration empty_decl1 = VariableDeclaration.CreateStatement("empty1", null, Undef.Create());
                VariableDeclaration empty_decl2 = VariableDeclaration.CreateStatement("empty2", null, null);
                root_ns.AddBuilder(FunctionBuilder.Create("testing",
                                NameFactory.UnitNameReference(),
                                Block.CreateStatement(
                                    empty_decl1,
                                    empty_decl2,
                                    ExpressionFactory.Readout("empty1"),
                                    ExpressionFactory.Readout("empty2")
                                )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalVariable, decl));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingTypeName, decl));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingTypeName, empty_decl1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingTypeAndValue, empty_decl2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnusedVariableWithinUsedExpression()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl = VariableDeclaration.CreateExpression("result", NameFactory.Int64NameReference(), initValue: Undef.Create());
                root_ns.AddBuilder(FunctionBuilder.Create("anything", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        // the declaration-expression is used, but the variable itself is not
                        // thus we report it as unused (in such code user should pass the init-value itself w/o creating variable)
                        ExpressionFactory.Readout(decl)
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl.Name));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReassigningFixedVariable()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression assignment = Assignment.CreateStatement(NameReference.Create("x"), Int64Literal.Create("5"));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("3")),
                    assignment,
                    ExpressionFactory.Readout("x") })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, assignment));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorCompoundDefaultValue()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true, AllowProtocols = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var decl = root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameReferenceUnion.Create(
                    new[] { NameFactory.PointerNameReference(NameFactory.BoolNameReference()),
                    NameFactory.PointerNameReference(NameFactory.Int64NameReference())}),
                    initValue: null, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotAutoInitializeCompoundType, decl));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter VariableBinding()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var decl_1 = VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(), Undef.Create(), modifier: EntityModifier.Public);
                root_ns.AddNode(decl_1);
                var var_1_ref = NameReference.Create("x");
                var decl_2 = root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.RealNameReference(),
                    var_1_ref, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);
                Assert.AreEqual(1, decl_1.TypeName.Binding().Matches.Count);
                Assert.AreEqual(env.Real64Type, decl_1.TypeName.Binding().Match.Instance.Target);

                Assert.AreEqual(1, var_1_ref.Binding.Matches.Count);
                Assert.AreEqual(decl_1, var_1_ref.Binding.Match.Instance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AssignmentTypeChecking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(), Undef.Create()));
                root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.RealNameReference(), NameReference.Create("x"),
                    modifier: EntityModifier.Public));
                var x_ref = NameReference.Create("x");
                root_ns.AddNode(VariableDeclaration.CreateStatement("z", NameFactory.Int64NameReference(), x_ref,
                    modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, x_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TypeInference()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var var_x = VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(), Undef.Create());
                var var_y = VariableDeclaration.CreateStatement("y", null, NameReference.Create("x"));

                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    var_x,
                    var_y,
                    ExpressionFactory.Readout("y")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

                Assert.IsTrue(env.Real64Type.InstanceOf.HasSameCore(var_y.Evaluation.Components));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctionAssignment()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                    })).Parameters(FunctionParameter.Create("t", NameFactory.Int64NameReference(), Variadic.None,
                        null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

                // x *IFunction<Int,Double> = foo
                root_ns.AddNode(VariableDeclaration.CreateStatement("x",
                    NameFactory.PointerNameReference(NameReference.Create(NameFactory.IFunctionTypeName, NameFactory.Int64NameReference(), NameFactory.RealNameReference())),
                    initValue: NameReference.Create("foo"), modifier: EntityModifier.Public));
                var foo_ref = NameReference.Create("foo");
                // y *IFunction<Double,Int> = foo
                VariableDeclaration decl = VariableDeclaration.CreateStatement("y",
                    NameFactory.PointerNameReference(NameReference.Create(NameFactory.IFunctionTypeName, NameFactory.RealNameReference(), NameFactory.Int64NameReference())),
                    initValue: foo_ref, modifier: EntityModifier.Public);
                root_ns.AddNode(decl);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, foo_ref.Binding.Matches.Count);
                Assert.AreEqual(func_def, foo_ref.Binding.Match.Instance.Target);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, decl.InitValue));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorVariableNotUsed()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration member = VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(),
                    Int64Literal.Create("5"));
                root_ns.AddBuilder(TypeBuilder.Create("Thing")
                    .With(member));

                var decl1 = VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null);
                var decl2 = VariableDeclaration.CreateStatement("t", NameFactory.Int64NameReference(), null);
                NameDefinition loop_label = NameDefinition.Create("here");
                var loop = Loop.CreateFor(loop_label,
                  init: null,
                  condition: null,
                  step: null,
                  body: new IExpression[] { decl1, decl2 });
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    loop,
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl1.Name));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, decl2.Name));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, loop_label));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, member.Name));
            }

            return resolver;
        }
    }
}