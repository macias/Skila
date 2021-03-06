﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class TypeMatchingTest : ITest
    {
        [TestMethod]
        public IErrorReporter DuckTypingOnEmptyInterface()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                    // we test this option here or more precisely duck typing
                    InterfaceDuckTyping = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                // please note it is not the interface is empty (it is not because IObject is not empty), 
                // but it does not add anything, so in this sense it is empty
                root_ns.AddBuilder(TypeBuilder.CreateInterface("IWhat"));

                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.PointerNameReference(NameFactory.IObjectNameReference()), Undef.Create()),
                        // should be legal despite duck typing, i.e. we should not error that the types are exchangable
                        // they are in sense of duck typing but checking if the type IS another type should be duck-free
                         ExpressionFactory.Readout(IsType.Create(NameReference.Create("x"), NameReference.Create("IWhat"))),
                        VariableDeclaration.CreateStatement("y", NameFactory.PointerNameReference(NameReference.Create("IWhat")), Undef.Create()),
                         ExpressionFactory.Readout(IsSame.Create(NameReference.Create("x"), NameReference.Create("y")))
                )));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIsTypeAlienSealed()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IWhat"));

                root_ns.AddBuilder(TypeBuilder.Create("What")
                    .Parents("IWhat"));

                // comparison does not make sense, because string is sealed and it is not possible to be given interface
                IsType is_type = IsType.Create(NameReference.Create("x"), NameFactory.StringNameReference());
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    NameFactory.BoolNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.PointerNameReference("IWhat"), Undef.Create()),
                        Return.Create(is_type)
                )));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, is_type));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMatchingIntersection()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowProtocols = true,
                    DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetPos")
                    .With(FunctionBuilder.CreateDeclaration("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetNeg")
                    .With(FunctionBuilder.CreateDeclaration("getMore", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("GetAll")
                    .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("3"))
                        })))
                    .With(FunctionBuilder.Create("getMore", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("-1"))
                        }))));

                NameReferenceIntersection intersection = NameReferenceIntersection.Create(
                    NameFactory.PointerNameReference(NameReference.Create("IGetNeg")),
                    NameFactory.PointerNameReference(NameReference.Create("IGetPos")));
                IExpression init_value =  ExpressionFactory.HeapConstructor("GetAll");
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",intersection, init_value),
                     ExpressionFactory.Readout("a")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter OutgoingConversion()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var type_foo_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                    .With(FunctionBuilder.Create(
                        NameFactory.ConvertFunctionName,
                        null, ExpressionReadMode.ReadRequired, NameReference.Create("Bar"),
                        Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) }))
                        .SetModifier(EntityModifier.Implicit))
                    // added second conversion to check if compiler correctly disambiguate the call
                    .With(FunctionBuilder.Create(
                        NameFactory.ConvertFunctionName,
                        null, ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) }))
                        .SetModifier(EntityModifier.Implicit)));
                var type_bar_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Bar")));


                root_ns.AddBuilder(FunctionBuilder.Create(
                    "wrapper",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f", NameReference.Create("Foo"),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("b", NameReference.Create("Bar"),
                        initValue: NameReference.Create("f")),
                     ExpressionFactory.Readout("b")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingValueType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true,
                    DiscardingAnyExpressionDuringTests = true
                }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.RealNameReference());
                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.IObjectNameReference(), initValue: Undef.Create(), modifier: EntityModifier.Public);
                var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                IsType is_type_ref = IsType.Create(NameReference.Create("u"), NameFactory.ISequenceNameReference("G"));
                root_ns.AddBuilder(FunctionBuilder.Create("more", "G", VarianceMode.None,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                     ExpressionFactory.Readout(is_type_ref)
                    ))
                    .Parameters(FunctionParameter.Create("u", NameFactory.ReferenceNameReference(
                        NameFactory.ISequenceNameReference("G", mutability: TypeMutability.ReadOnly)))));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.IsTypeOfKnownTypes, is_type));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.IsTypeOfKnownTypes, is_type_ref));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingKnownTypes()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerNameReference(NameFactory.IObjectNameReference()));
                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerNameReference(NameFactory.RealNameReference()), initValue: Undef.Create(), modifier: EntityModifier.Public);
                var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.IsTypeOfKnownTypes, is_type));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingMismatchedTypes()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerNameReference(NameFactory.Int64NameReference()));
                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerNameReference(NameFactory.RealNameReference()),
                    initValue: Undef.Create(), modifier: EntityModifier.Public);
                var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, is_type));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter TypeTesting()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerNameReference(NameFactory.RealNameReference()));
                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerNameReference(NameFactory.IObjectNameReference()),
                    initValue: Undef.Create(), modifier: EntityModifier.Public);
                var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorMixingSlicingTypes()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowProtocols = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                INameReference typename = NameReferenceUnion.Create(new[] {
                NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                NameFactory.BoolNameReference() });
                var decl = VariableDeclaration.CreateStatement("foo", typename, initValue: Undef.Create());
                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    decl,
                     ExpressionFactory.Readout("foo")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MixingSlicingTypes, typename));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorPassingValues()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.RealNameReference(), initValue: Undef.Create(), modifier: EntityModifier.Public);
                NameReference foo_ref = NameReference.Create("foo");
                var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.IObjectNameReference(),
                    initValue: foo_ref, modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, foo_ref));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AssigningUndef()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealNameReference(), Undef.Create(),
                    modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);
                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PassingPointers()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerNameReference(NameFactory.RealNameReference()),
                    initValue: Undef.Create(), modifier: EntityModifier.Public);
                var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.PointerNameReference(NameFactory.IObjectNameReference()),
                    initValue: NameReference.Create("foo"), modifier: EntityModifier.Public);
                root_ns.AddNode(decl_src);
                root_ns.AddNode(decl_dst);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter InheritanceMatching()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var unrelated_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Separate")));
                var abc_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("ABC")));
                var derived_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Deriv"))
                    .Parents(NameReference.Create("ABC")));
                var foo_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "V", VarianceMode.Out))
                    .Parents(NameReference.Create("ABC")));
                var tuple_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Tuple", "T", VarianceMode.None))
                    .Parents(NameReference.Create("Foo", NameReference.Create("T"))));


                var separate_ref = system_ns.AddNode(NameReference.Create("Separate"));
                var abc_ref = system_ns.AddNode(NameReference.Create("ABC"));
                var deriv_ref = system_ns.AddNode(NameReference.Create("Deriv"));
                var tuple_deriv_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("Deriv")));
                var foo_abc_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("ABC")));
                var tuple_abc_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("ABC")));
                var foo_deriv_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("Deriv")));

                resolver = NameResolver.Create(env);

                Assert.AreNotEqual(TypeMatch.Same, separate_ref.Binding.Match.Instance.MatchesTarget(resolver.Context, abc_ref.Binding.Match.Instance,
                    TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true)));
                Assert.AreEqual(TypeMatch.Substitute, deriv_ref.Binding.Match.Instance.MatchesTarget(resolver.Context, abc_ref.Binding.Match.Instance,
                    TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true)));
                Assert.AreEqual(TypeMatch.Substitute, tuple_deriv_ref.Binding.Match.Instance.MatchesTarget(resolver.Context, foo_abc_ref.Binding.Match.Instance,
                    TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true)));
                TypeMatch match = tuple_abc_ref.Binding.Match.Instance.MatchesTarget(resolver.Context, foo_deriv_ref.Binding.Match.Instance,
                    TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true));
                Assert.AreNotEqual(TypeMatch.Same, match);
                Assert.AreNotEqual(TypeMatch.Substitute, match);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ConstraintsMatching()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var base_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Basic")));
                var abc_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("ABC"))
                    .Parents(NameReference.Create("Basic")));
                var derived_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Deriv"))
                    .Parents(NameReference.Create("ABC")));
                var foo_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo",
                        TemplateParametersBuffer.Create().Add("V", VarianceMode.Out).Values))
                    .Constraints(ConstraintBuilder.Create("V").Inherits(NameReference.Create("ABC")))
                    .Parents(NameReference.Create("ABC")));
                var tuple_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Tuple",
                    TemplateParametersBuffer.Create().Add("T", VarianceMode.None).Values))
                    .Constraints(ConstraintBuilder.Create("T").BaseOf(NameReference.Create("ABC"))));

                var tuple_ok_type = TypeBuilder.Create(
                    NameDefinition.Create("TupleOK", TemplateParametersBuffer.Create().Add("U", VarianceMode.None).Values))
                    .Constraints(ConstraintBuilder.Create("U").BaseOf(NameReference.Create("Basic")))
                    .Parents(NameReference.Create("Tuple", NameReference.Create("U"))).Build();
                system_ns.AddNode(tuple_ok_type);
                var tuple_bad_type = TypeBuilder.Create(
                    NameDefinition.Create("TupleBad", TemplateParametersBuffer.Create().Add("L", VarianceMode.None).Values))
                    .Parents(NameReference.Create("Tuple", NameReference.Create("L"))).Build();
                system_ns.AddNode(tuple_bad_type);

                var foo_deriv_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("Deriv")));
                var tuple_basic_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("Basic")));
                var foo_basic_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("Basic")));
                var tuple_deriv_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("Deriv")));

                resolver = NameResolver.Create(env);

                // constraints are matched
                Assert.AreEqual(1, foo_deriv_ref.Binding.Matches.Count());
                Assert.AreEqual(foo_type, foo_deriv_ref.Binding.Match.Instance.Target);

                Assert.AreEqual(1, tuple_basic_ref.Binding.Matches.Count());
                Assert.AreEqual(tuple_type, tuple_basic_ref.Binding.Match.Instance.Target);

                // failed on constraints 
                Assert.AreEqual(0, foo_basic_ref.Binding.Matches.Count());

                Assert.AreEqual(0, tuple_deriv_ref.Binding.Matches.Count());

                // constraints matching other constraints
                Assert.AreEqual(1, tuple_ok_type.ParentNames.Single().Binding.Matches.Count);
                Assert.AreEqual(tuple_type, tuple_ok_type.ParentNames.Single().Binding.Match.Instance.Target);

                Assert.AreEqual(0, tuple_bad_type.ParentNames.Single().Binding.Matches.Count);
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter UnionMatching()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Separate")));
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("ABC")));
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Deriv"))
                    .Parents(NameReference.Create("ABC")));
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Deriz"))
                    .Parents(NameReference.Create("Deriv")));
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("qwerty"))
                    .Parents(NameReference.Create("ABC")));
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("sink"))
                    .Parents(NameReference.Create("qwerty"), NameReference.Create("Separate")));


                var separate_deriv_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("Deriv")));
                var separate_deriz_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("Deriz")));
                var separate_abc_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("ABC")));
                var sink_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("sink")));
                var sink_deriv_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("sink"), NameReference.Create("Deriv")));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(TypeMatch.Substitute, separate_deriz_union.Evaluation.Components.MatchesTarget(resolver.Context,
                    separate_deriv_union.Evaluation.Components, TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true)));
                Assert.AreEqual(TypeMatch.Substitute, sink_union.Evaluation.Components.MatchesTarget(resolver.Context,
                    separate_abc_union.Evaluation.Components, TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true)));
                TypeMatch match = sink_deriv_union.Evaluation.Components.MatchesTarget(resolver.Context,
                    separate_deriz_union.Evaluation.Components, TypeMatching.Create(env.Options.InterfaceDuckTyping, allowSlicing: true));
                Assert.AreNotEqual(TypeMatch.Same, match);
                Assert.AreNotEqual(TypeMatch.Substitute, match);
            }

            return resolver;
        }
    }

}
