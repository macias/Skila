using System;
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
    public class TypeMatching
    {
        [TestMethod]
        public IErrorReporter OutgoingConversion()
        {
            var env = Language.Environment.Create();
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
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("f", NameReference.Create("Foo"),
                        initValue: Undef.Create()),
                    VariableDeclaration.CreateStatement("b", NameReference.Create("Bar"),
                        initValue: NameReference.Create("f")),
                    Tools.Readout("b")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingValueType()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.DoubleTypeReference());
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.ObjectTypeReference(), initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.IsTypeOfKnownTypes, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(is_type, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingKnownTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()), initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.IsTypeOfKnownTypes, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(is_type, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorTestingMismatchedTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.IntTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(is_type, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter TypeTesting()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            IsType is_type = IsType.Create(NameReference.Create("foo"), NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()));
            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", null, initValue: is_type);
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorMixingSlicingTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            INameReference typename = NameReferenceUnion.Create(new[] {
                NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                NameFactory.BoolTypeReference() });
            var decl = VariableDeclaration.CreateStatement("foo", typename, initValue: Undef.Create());
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { decl, Tools.Readout("foo") })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.MixingSlicingTypes, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(typename, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorPassingValues()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.DoubleTypeReference(), initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ObjectTypeReference(),
                initValue: NameReference.Create("foo"));
            root_ns.AddNode(decl_src);
            root_ns.AddNode(decl_dst);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AssigningUndef()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(), Undef.Create()));

            var resolver = NameResolver.Create(env);
            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PassingPointers()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.DoubleTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()),
                initValue: NameReference.Create("foo"));
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
            var derived_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Deriv")).Parents(NameReference.Create("ABC")));
            var foo_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "V", VarianceMode.Out)).Parents(NameReference.Create("ABC")));
            var tuple_type = system_ns.AddBuilder(TypeBuilder.Create(
                NameDefinition.Create("Tuple", "T", VarianceMode.None))
                .Parents(NameReference.Create("Foo", NameReference.Create("T"))));


            var separate_ref = system_ns.AddNode(NameReference.Create("Separate"));
            var abc_ref = system_ns.AddNode(NameReference.Create("ABC"));
            var deriv_ref = system_ns.AddNode(NameReference.Create("Deriv"));
            var tuple_deriv_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("Deriv")));
            var foo_abc_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("ABC")));
            var tuple_abc_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("ABC")));
            var foo_deriv_ref = system_ns.AddNode(NameReference.Create("Foo", NameReference.Create("Deriv")));

            var resolver = NameResolver.Create(env);

            Assert.AreNotEqual(TypeMatch.Pass, separate_ref.Binding.Match.MatchesTarget(resolver.Context, abc_ref.Binding.Match, allowSlicing: true));
            Assert.AreEqual(TypeMatch.Pass, deriv_ref.Binding.Match.MatchesTarget(resolver.Context, abc_ref.Binding.Match, allowSlicing: true));
            Assert.AreEqual(TypeMatch.Pass, tuple_deriv_ref.Binding.Match.MatchesTarget(resolver.Context, foo_abc_ref.Binding.Match, allowSlicing: true));
            Assert.AreNotEqual(TypeMatch.Pass, tuple_abc_ref.Binding.Match.MatchesTarget(resolver.Context, foo_deriv_ref.Binding.Match, allowSlicing: true));

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

            var unrelated_type = TypeBuilder.Create(
                NameDefinition.Create("Separate")).Build();
            root_ns.AddNode(unrelated_type);
            var abc_type = TypeBuilder.Create(
                NameDefinition.Create("ABC")).Build();
            root_ns.AddNode(abc_type);
            var derived_type = TypeBuilder.Create(NameDefinition.Create("Deriv")).Parents(NameReference.Create("ABC")).Build();
            root_ns.AddNode(derived_type);
            var deriz_type = TypeBuilder.Create(NameDefinition.Create("Deriz")).Parents(NameReference.Create("Deriv")).Build();
            root_ns.AddNode(deriz_type);
            var qwerty_type = TypeBuilder.Create(NameDefinition.Create("qwerty")).Parents(NameReference.Create("ABC")).Build();
            root_ns.AddNode(qwerty_type);
            var sink_type = TypeBuilder.Create(NameDefinition.Create("sink"))
                .Parents(NameReference.Create("qwerty"), NameReference.Create("Separate")).Build();
            root_ns.AddNode(sink_type);


            var separate_deriv_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("Deriv")));
            var separate_deriz_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("Deriz")));
            var separate_abc_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("Separate"), NameReference.Create("ABC")));
            var sink_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("sink")));
            var sink_deriv_union = root_ns.AddNode(NameReferenceUnion.Create(NameReference.Create("sink"), NameReference.Create("Deriv")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(TypeMatch.Pass, separate_deriz_union.Evaluation.Components.MatchesTarget(resolver.Context, 
                separate_deriv_union.Evaluation.Components, allowSlicing: true));
            Assert.AreEqual(TypeMatch.Pass, sink_union.Evaluation.Components.MatchesTarget(resolver.Context, 
                separate_abc_union.Evaluation.Components, allowSlicing: true));
            Assert.AreNotEqual(TypeMatch.Pass, sink_deriv_union.Evaluation.Components.MatchesTarget(resolver.Context, 
                separate_deriz_union.Evaluation.Components, allowSlicing: true));

            return resolver;
        }
    }

}
