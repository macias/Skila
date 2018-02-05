using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class TypeMatchingTest
    {
        [TestMethod]
        public IErrorReporter ErrorIsTypeAlienSealed()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IWhat"));

            root_ns.AddBuilder(TypeBuilder.Create("What")
                .Parents("IWhat"));

            // comparison does not make sense, because string is sealed and it is not possible to be given interface
            IsType is_type = IsType.Create(NameReference.Create("x"), NameFactory.StringTypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("x", NameFactory.PointerTypeReference("IWhat"), Undef.Create()),
                    Return.Create(is_type)
            )));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, is_type));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMatchingIntersection()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true   });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetPos")
                .With(FunctionBuilder.CreateDeclaration("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.CreateInterface("IGetNeg")
                .With(FunctionBuilder.CreateDeclaration("getMore", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("GetAll")
                .With(FunctionBuilder.Create("getSome", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("3"))
                    })))
                .With(FunctionBuilder.Create("getMore", ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("-1"))
                    }))));

            NameReferenceIntersection intersection = NameReferenceIntersection.Create(
                NameFactory.PointerTypeReference(NameReference.Create("IGetNeg")),
                NameFactory.PointerTypeReference(NameReference.Create("IGetPos")));
            IExpression init_value = ExpressionFactory.HeapConstructor("GetAll");
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",intersection, init_value),
                    ExpressionFactory.Readout("a")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter OutgoingConversion()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;

            var type_foo_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo"))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create(NameFactory.ConvertFunctionName),
                    null, ExpressionReadMode.ReadRequired, NameReference.Create("Bar"),
                    Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) }))
                    .Modifier(EntityModifier.Implicit))
                // added second conversion to check if compiler correctly disambiguate the call
                .With(FunctionBuilder.Create(
                    NameDefinition.Create(NameFactory.ConvertFunctionName),
                    null, ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new IExpression[] { Return.Create(Undef.Create()) }))
                    .Modifier(EntityModifier.Implicit)));
            var type_bar_def = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Bar")));


            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("wrapper"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f", NameReference.Create("Foo"),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("b", NameReference.Create("Bar"),
                        initValue: NameReference.Create("f")),
                    ExpressionFactory.Readout("b")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingValueType()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.DoubleTypeReference());
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.ObjectTypeReference(), initValue: Undef.Create(), modifier: EntityModifier.Public);
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.IsTypeOfKnownTypes, is_type));
            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingKnownTypes()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()), initValue: Undef.Create(), modifier: EntityModifier.Public);
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.IsTypeOfKnownTypes, is_type));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingMismatchedTypes()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.IntTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Public);
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, is_type));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter TypeTesting()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Public);
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type, modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorMixingSlicingTypes()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            INameReference typename = NameReferenceUnion.Create(new[] {
                NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                NameFactory.BoolTypeReference() });
            var decl = VariableDeclaration.CreateStatement("foo", typename, initValue: Undef.Create());
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("foo")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.MixingSlicingTypes, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(typename, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorPassingValues()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.DoubleTypeReference(), initValue: Undef.Create(), modifier: EntityModifier.Public);
            NameReference foo_ref = NameReference.Create("foo");
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ObjectTypeReference(),
                initValue: foo_ref, modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, foo_ref));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AssigningUndef()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create(), 
                modifier: EntityModifier.Public));

            var resolver = NameResolver.Create(env);
            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PassingPointers()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Public);
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()),
                initValue: NameReference.Create("foo"), modifier: EntityModifier.Public);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter InheritanceMatching()
        {
            var env = Language.Environment.Create();
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

            var resolver = NameResolver.Create(env);

            Assert.AreNotEqual(TypeMatch.Same, separate_ref.Binding.Match.MatchesTarget(resolver.Context, abc_ref.Binding.Match, TypeMatching.Create(allowSlicing: true)));
            Assert.AreEqual(TypeMatch.Substitute, deriv_ref.Binding.Match.MatchesTarget(resolver.Context, abc_ref.Binding.Match, TypeMatching.Create(allowSlicing: true)));
            Assert.AreEqual(TypeMatch.Substitute, tuple_deriv_ref.Binding.Match.MatchesTarget(resolver.Context, foo_abc_ref.Binding.Match, TypeMatching.Create(allowSlicing: true)));
            TypeMatch match = tuple_abc_ref.Binding.Match.MatchesTarget(resolver.Context, foo_deriv_ref.Binding.Match, TypeMatching.Create(allowSlicing: true));
            Assert.AreNotEqual(TypeMatch.Same, match);
            Assert.AreNotEqual(TypeMatch.Substitute, match);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ConstraintsMatching()
        {
            var env = Language.Environment.Create();
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

            var resolver = NameResolver.Create(env);

            // constraints are matched
            Assert.AreEqual(1, foo_deriv_ref.Binding.Matches.Count());
            Assert.AreEqual(foo_type, foo_deriv_ref.Binding.Match.Target);

            Assert.AreEqual(1, tuple_basic_ref.Binding.Matches.Count());
            Assert.AreEqual(tuple_type, tuple_basic_ref.Binding.Match.Target);

            // failed on constraints 
            Assert.AreEqual(0, foo_basic_ref.Binding.Matches.Count());

            Assert.AreEqual(0, tuple_deriv_ref.Binding.Matches.Count());

            // constraints matching other constraints
            Assert.AreEqual(1, tuple_ok_type.ParentNames.Single().Binding.Matches.Count);
            Assert.AreEqual(tuple_type, tuple_ok_type.ParentNames.Single().Binding.Match.Target);

            Assert.AreEqual(0, tuple_bad_type.ParentNames.Single().Binding.Matches.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter UnionMatching()
        {
            var env = Language.Environment.Create();
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(TypeMatch.Substitute, separate_deriz_union.Evaluation.Components.MatchesTarget(resolver.Context,
                separate_deriv_union.Evaluation.Components, TypeMatching.Create(allowSlicing: true)));
            Assert.AreEqual(TypeMatch.Substitute, sink_union.Evaluation.Components.MatchesTarget(resolver.Context,
                separate_abc_union.Evaluation.Components, TypeMatching.Create(allowSlicing: true)));
            TypeMatch match = sink_deriv_union.Evaluation.Components.MatchesTarget(resolver.Context,
                separate_deriz_union.Evaluation.Components, TypeMatching.Create(allowSlicing: true));
            Assert.AreNotEqual(TypeMatch.Same, match);
            Assert.AreNotEqual(TypeMatch.Substitute, match);

            return resolver;
        }
    }

}
