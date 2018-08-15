using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System.Linq;
using Skila.Language.Extensions;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Templates : ITest
    {   
        [TestMethod]
        public IErrorReporter ErrorDisabledProtocols()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                // just testing if disabling protocols (default) option really works
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference());

                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(), Block.CreateStatement())
                    .Constraints(ConstraintBuilder.Create("T").Has(func_constraint)));

                resolver = NameResolver.Create(env);

                TypeDefinition template_type = func.NestedTypes().Single();
                EntityModifier type_modifier = template_type.Modifier;

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DisabledProtocols, type_modifier));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PassingTraitAsIncorrectInterface()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .SetModifier(EntityModifier.Trait)
                    .Parents("ISay")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                        Block.CreateStatement(
                            Return.Create(Int64Literal.Create("2"))
                        )).SetModifier(EntityModifier.Override)));

                IExpression init_value =  ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")));
                root_ns.AddBuilder(FunctionBuilder.Create("major",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("g", NameFactory.PointerNameReference("ISay"),
                            init_value),
                         ExpressionFactory.Readout("g")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingTraitMethodOnHost()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                    .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference())));

                root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

                // this function is located in trait, thus unavailable
                FunctionCall int_call = FunctionCall.Create(NameReference.CreateThised("hello"));
                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T")
                    .With(FunctionBuilder.Create("reaching", NameFactory.UnitNameReference(),
                        Block.CreateStatement(int_call))));

                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                    .SetModifier(EntityModifier.Trait)
                    .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                    .With(FunctionBuilder.Create("hello", ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        Return.Create(Int64Literal.Create("7"))
                    ))));

                FunctionCall ext_call = FunctionCall.Create(NameReference.Create("g", "hello"));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("g", null,
                             ExpressionFactory.StackConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")))),
                        Return.Create(ext_call)
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, ext_call.Name));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, int_call.Name));
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorMisplacedConstraint()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                TemplateConstraint constraints = ConstraintBuilder.Create("BOO").SetModifier(EntityModifier.Const);
                root_ns.AddBuilder(TypeBuilder.Create("Greeter", "BOO")
                    .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.UnitNameReference(),
                        Block.CreateStatement())
                    .Constraints(constraints)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MisplacedConstraint, constraints));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTraitDefinition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Base));

                TypeDefinition non_generic_trait = root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Trait));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None)));

                TypeDefinition unconstrained_trait = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None))
                    .SetModifier(EntityModifier.Trait));

                TypeDefinition missing_host = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("MissMe", "Y", VarianceMode.None))
                    .SetModifier(EntityModifier.Trait)
                    .Constraints(ConstraintBuilder.Create("Y")
                        .SetModifier(EntityModifier.Const)));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Almost", "T", VarianceMode.None)));

                FunctionDefinition trait_constructor = FunctionBuilder.CreateInitConstructor(Block.CreateStatement());
                VariableDeclaration trait_field = VariableDeclaration.CreateStatement("f", NameFactory.Int64NameReference(), Int64Literal.Create("5"), EntityModifier.Public);
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Almost", "T", VarianceMode.None))
                    .SetModifier(EntityModifier.Trait)
                    .With(trait_constructor)
                    .With(trait_field)
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(EntityModifier.Const)));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Inheriting", "T", VarianceMode.None)));

                NameReference parent_impl = NameReference.Create("Bar");
                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Inheriting", "T", VarianceMode.None))
                    .Parents(parent_impl)
                    .SetModifier(EntityModifier.Trait)
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(EntityModifier.Const)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(6, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonGenericTrait, non_generic_trait));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UnconstrainedTrait, unconstrained_trait));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingHostTypeForTrait, missing_host));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TraitConstructor, trait_constructor));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TraitInheritingTypeImplementation, parent_impl));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.FieldInNonImplementationType, trait_field));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicTraitDefinition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None)));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None))
                    .SetModifier(EntityModifier.Trait)
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(EntityModifier.Const)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TranslationTableOfInferredCommonTypes()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                TemplateParameter template_param = TemplateParametersBuffer.Create("T").Values.Single();
                root_ns.AddBuilder(FunctionBuilder.Create("getMe", new[] { template_param },
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(), Block.CreateStatement())
                        .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceNameReference("T"), ExpressionReadMode.CannotBeRead),
                            FunctionParameter.Create("b", NameFactory.ReferenceNameReference("T"), ExpressionReadMode.CannotBeRead)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("getMe"), NameReference.Create("x"), NameReference.Create("y"));
                root_ns.AddBuilder(FunctionBuilder.Create("common",
                        NameFactory.UnitNameReference(),
                        Block.CreateStatement(
                            VariableDeclaration.CreateStatement("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                                Int64Literal.Create("3")),
                            VariableDeclaration.CreateStatement("y", NameFactory.ReferenceNameReference(NameFactory.BoolNameReference()),
                                BoolLiteral.CreateTrue()),
                            call
                            )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                // the actual point of this test are those two lines checking if we get correct translation table for entire
                // instance of the called function
                Assert.IsTrue(call.Resolution.TargetFunctionInstance.Translation.Translate(template_param,
                    out IEntityInstance common_instance));
                Assert.AreEqual(resolver.Context.Env.IEquatableType.InstanceOf, common_instance);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InternalDirectTranslationTables()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { MiniEnvironment = true }.SetMutability(mutability));
                var root_ns = env.Root;

                const string parent_typename = "Oldman";
                const string parent_elemtype = "PT";

                FunctionDefinition base_func = FunctionBuilder.CreateDeclaration("getMe",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.ReferenceNameReference(parent_elemtype));
                TypeDefinition parent = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(parent_typename,
                    TemplateParametersBuffer.Create(parent_elemtype).Values))
                    .SetModifier(EntityModifier.Abstract)
                    .With(base_func));

                const string child_typename = "Kid";
                const string child_elemtype = "CT";

                FunctionDefinition deriv_func = FunctionBuilder.Create("getMe",
                        ExpressionReadMode.CannotBeRead,
                        NameFactory.ReferenceNameReference(child_elemtype),
                        Block.CreateStatement(Return.Create(Undef.Create())))
                        .SetModifier(EntityModifier.Override);
                TypeDefinition child = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(child_typename,
                        TemplateParametersBuffer.Create(child_elemtype).Values))
                    .Parents(NameReference.Create(parent_typename, NameReference.Create(child_elemtype)))
                    .With(deriv_func));

                resolver = NameResolver.Create(env);

                // testing here template translation
                EntityInstance child_ancestor = child.Inheritance.OrderedAncestorsWithoutObject.Single();
                IEntityInstance translated = base_func.ResultTypeName.Evaluation.Components.TranslateThrough(child_ancestor);

                // we have single function overriden, so it is easy to debug and spot if something goes wrong
                bool result = FunctionDefinitionExtension.IsDerivedOf(resolver.Context, deriv_func, base_func, child_ancestor);

                Assert.IsTrue(result);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InternalIndirectTranslationTables()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { MiniEnvironment = true, DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                const string proxy_typename = "Proxy";
                const string proxy_elemtype = "X";

                FunctionDefinition get_func = FunctionBuilder.Create("getMe",
                    ExpressionReadMode.OptionalUse,
                    NameReference.Create(proxy_elemtype),
                    Block.CreateStatement(Return.Create(Undef.Create())));
                TypeDefinition proxy = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(proxy_typename,
                    TemplateParametersBuffer.Create(proxy_elemtype).Values))
                    .With(get_func));

                const string parent_typename = "Oldman";
                const string parent_elemtype = "PT";

                FunctionDefinition access_func = FunctionBuilder.Create("provide",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceNameReference(NameReference.Create(proxy_typename, NameReference.Create(parent_elemtype))),
                    Block.CreateStatement(Return.Create(Undef.Create())));
                TypeDefinition parent = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(parent_typename,
                    TemplateParametersBuffer.Create(parent_elemtype).Values))
                    .SetModifier(EntityModifier.Base)
                    .With(access_func)
                    );

                const string child_typename = "Kid";
                const string child_elemtype = "CT";

                FunctionCall call = FunctionCall.Create(NameReference.Create("i", "getMe"));
                // with buggy template translation table we would have here a type mismatch error
                // if this error happens check first if prefix of the call is evaluated correctly
                VariableDeclaration assignment = VariableDeclaration.CreateStatement("e", NameReference.Create(child_elemtype), call);

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(child_typename,
                        TemplateParametersBuffer.Create(child_elemtype).Values))
                    .Parents(NameReference.Create(parent_typename, NameReference.Create(child_elemtype)))
                    .With(FunctionBuilder.Create("process",
                        ExpressionReadMode.CannotBeRead,
                        NameFactory.UnitNameReference(),
                        Block.CreateStatement(
                            VariableDeclaration.CreateStatement("i",
                                NameFactory.ReferenceNameReference(NameReference.Create(proxy_typename, NameReference.Create(child_elemtype))),
                                FunctionCall.Create(NameReference.CreateThised("provide"))),
                             ExpressionFactory.Readout("i"),

                            assignment,
                             ExpressionFactory.Readout("e")
                            ))));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorPassingReferenceAsTypeArgument()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement()));

                NameReference function_name = NameReference.Create("proxy",
                    NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()));

                root_ns.AddBuilder(FunctionBuilder.Create("tester",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        FunctionCall.Create(function_name)
                        )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceAsTypeArgument, function_name.TemplateArguments.Single()));
            }

            return resolver;
        }


        [TestMethod]
        public IErrorReporter InferredPartialTemplateArgumentsOnConstraints()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("part",
                    TemplateParametersBuffer.Create("T", "X").Values,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement())
                    .Constraints(ConstraintBuilder.Create("X")
                        .BaseOf(NameReference.Create("T"))));

                FunctionCall call = FunctionCall.Create(NameReference.Create("part", NameFactory.Int64NameReference(),
                    NameFactory.SinkReference()));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "caller",
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(new IExpression[] {
                    call
                    })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[0].TypeName.Evaluation.Components);
                Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[1].TypeName.Evaluation.Components);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InferredTemplateArgumentsOnConstraints()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("part",
                    TemplateParametersBuffer.Create("T", "X").Values,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement())
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), ExpressionReadMode.CannotBeRead))
                    .Constraints(ConstraintBuilder.Create("X")
                        .BaseOf(NameReference.Create("T"))));

                FunctionCall call = FunctionCall.Create(NameReference.Create("part"), Int64Literal.Create("5"));
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "caller",
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(new IExpression[] {
                    call
                    })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[0].TypeName.Evaluation.Components);
                Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[1].TypeName.Evaluation.Components);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingConstConstraint()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Mut").SetModifier(EntityModifier.Mutable));

                NameReference parent_constraint = NameReference.Create("Mut");
                root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement())
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(EntityModifier.Const)
                        .Inherits(parent_constraint)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_constraint));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_constraint));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingTypesConstraint()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Parent")
                    .SetModifier(EntityModifier.Base));
                root_ns.AddBuilder(TypeBuilder.Create("Child").Parents("Parent"));

                NameReference baseof_name = NameReference.Create("Parent");
                NameReference parent_name = NameReference.Create("Child");
                root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement())
                    .Constraints(ConstraintBuilder.Create("T")
                        .BaseOf(baseof_name)
                        .Inherits(parent_name)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConstraintConflictingTypeHierarchy, baseof_name));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_name));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorHasConstraint()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true, AllowProtocols = true }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference());

                // this function accepts any parameter where parameter type has function "getMe"
                FunctionDefinition constrained_func = root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                    TemplateParametersBuffer.Create().Add("T").Values,
                    ExpressionReadMode.ReadRequired, NameFactory.Int64NameReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                         }))
                         .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                         .Parameters(FunctionParameter.Create("t", NameFactory.PointerNameReference("T"))));

                // this type does NOT have function "getMe"
                TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("YMan")
                    .With(FunctionBuilder.Create("missing",
                        ExpressionReadMode.ReadRequired,
                        NameFactory.Int64NameReference(),
                        Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                        }))));

                FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y_man")));
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y_man",null, ExpressionFactory.HeapConstructor(NameReference.Create("YMan"))),
                    Return.Create(call)
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedHasFunctionConstraint, call.Callee));
            }

            return resolver;
        }

    }
}
