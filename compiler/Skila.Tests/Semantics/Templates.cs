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
    public class Templates
    {
        [TestMethod]
        public IErrorReporter ErrorAssigningToNonReassignableData()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            IExpression assign = Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b"));

            root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    assign
                ))
                .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                    FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningToNonReassignableData, assign));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorSwapNonReassignableValues()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                    Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                    Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                ))
                .Constraints(ConstraintBuilder.Create("T")
                    .SetModifier(EntityModifier.Reassignable))
                .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                    FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));


            FunctionCall swap_call = FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b"));

            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("a", null, Nat8Literal.Create("2")),
                    VariableDeclaration.CreateStatement("b", null, Nat8Literal.Create("17")),
                    // error: both values are const
                    swap_call,
                    Return.Create(ExpressionFactory.Sub("a", "b"))
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedAssignabilityConstraint, swap_call.Name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDisabledProtocols()
        {
            // just testing if disabling protocols (default) option really works
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference());

            FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(), Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("T").Has(func_constraint)));

            var resolver = NameResolver.Create(env);

            TypeDefinition template_type = func.NestedTypes().Single();
            EntityModifier type_modifier = template_type.Modifier;

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DisabledProtocols, type_modifier));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PassingTraitAsIncorrectInterface()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T"));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .SetModifier(EntityModifier.Trait)
                .Parents("ISay")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(
                        Return.Create(Int64Literal.Create("2"))
                    )).SetModifier(EntityModifier.Override)));

            IExpression init_value = ExpressionFactory.HeapConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")));
            root_ns.AddBuilder(FunctionBuilder.Create("major",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("g", NameFactory.PointerTypeReference("ISay"),
                        init_value),
                    ExpressionFactory.Readout("g")
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingTraitMethodOnHost()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISay")
                .With(FunctionBuilder.CreateDeclaration("say", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference())));

            root_ns.AddBuilder(TypeBuilder.Create("NoSay"));

            // this function is located in trait, thus unavailable
            FunctionCall int_call = FunctionCall.Create(NameReference.CreateThised("hello"));
            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "T")
                .With(FunctionBuilder.Create("reaching", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(int_call))));

            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "X")
                .SetModifier(EntityModifier.Trait)
                .Constraints(ConstraintBuilder.Create("X").Inherits("ISay"))
                .With(FunctionBuilder.Create("hello", ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    Return.Create(Int64Literal.Create("7"))
                ))));

            FunctionCall ext_call = FunctionCall.Create(NameReference.Create("g", "hello"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("g", null,
                        ExpressionFactory.StackConstructor(NameReference.Create("Greeter", NameReference.Create("NoSay")))),
                    Return.Create(ext_call)
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, ext_call.Name));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, int_call.Name));

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ErrorMisplacedConstraint()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            TemplateConstraint constraints = ConstraintBuilder.Create("BOO").SetModifier(EntityModifier.Const);
            root_ns.AddBuilder(TypeBuilder.Create("Greeter", "BOO")
                .With(FunctionBuilder.Create("say", ExpressionReadMode.ReadRequired, NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                .Constraints(constraints)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MisplacedConstraint, constraints));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTraitDefinition()
        {
            var env = Environment.Create(new Options() { });
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
            VariableDeclaration trait_field = VariableDeclaration.CreateStatement("f", NameFactory.Int64TypeReference(), Int64Literal.Create("5"), EntityModifier.Public);
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(6, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonGenericTrait, non_generic_trait));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UnconstrainedTrait, unconstrained_trait));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingHostTypeForTrait, missing_host));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TraitConstructor, trait_constructor));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TraitInheritingTypeImplementation, parent_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.FieldInNonImplementationType, trait_field));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BasicTraitDefinition()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None)));

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Foo", "T", VarianceMode.None))
                .SetModifier(EntityModifier.Trait)
                .Constraints(ConstraintBuilder.Create("T")
                    .SetModifier(EntityModifier.Const)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TranslationTableOfInferredCommonTypes()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            TemplateParameter template_param = TemplateParametersBuffer.Create("T").Values.Single();
            root_ns.AddBuilder(FunctionBuilder.Create("getMe", new[] { template_param },
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(), Block.CreateStatement())
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T"), ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"), ExpressionReadMode.CannotBeRead)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("getMe"), NameReference.Create("x"), NameReference.Create("y"));
            root_ns.AddBuilder(FunctionBuilder.Create("common",
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()),
                            Int64Literal.Create("3")),
                        VariableDeclaration.CreateStatement("y", NameFactory.ReferenceTypeReference(NameFactory.BoolTypeReference()),
                            BoolLiteral.CreateTrue()),
                        call
                        )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            // the actual point of this test are those two lines checking if we get correct translation table for entire
            // instance of the called function
            Assert.IsTrue(call.Resolution.TargetFunctionInstance.Translation.Translate(template_param,
                out IEntityInstance common_instance));
            Assert.AreEqual(resolver.Context.Env.IEquatableType.InstanceOf, common_instance);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InternalDirectTranslationTables()
        {
            var env = Environment.Create(new Options() { MiniEnvironment = true });
            var root_ns = env.Root;

            const string parent_typename = "Oldman";
            const string parent_elemtype = "PT";

            FunctionDefinition base_func = FunctionBuilder.CreateDeclaration("getMe",
                ExpressionReadMode.CannotBeRead,
                NameFactory.ReferenceTypeReference(parent_elemtype));
            TypeDefinition parent = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(parent_typename,
                TemplateParametersBuffer.Create(parent_elemtype).Values))
                .SetModifier(EntityModifier.Abstract)
                .With(base_func));

            const string child_typename = "Kid";
            const string child_elemtype = "CT";

            FunctionDefinition deriv_func = FunctionBuilder.Create("getMe",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.ReferenceTypeReference(child_elemtype),
                    Block.CreateStatement(Return.Create(Undef.Create())))
                    .SetModifier(EntityModifier.Override);
            TypeDefinition child = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(child_typename,
                    TemplateParametersBuffer.Create(child_elemtype).Values))
                .Parents(NameReference.Create(parent_typename, NameReference.Create(child_elemtype)))
                .With(deriv_func));

            var resolver = NameResolver.Create(env);

            // testing here template translation
            EntityInstance child_ancestor = child.Inheritance.OrderedAncestorsWithoutObject.Single();
            IEntityInstance translated = base_func.ResultTypeName.Evaluation.Components.TranslateThrough(child_ancestor);

            // we have single function overriden, so it is easy to debug and spot if something goes wrong
            bool result = FunctionDefinitionExtension.IsDerivedOf(resolver.Context, deriv_func, base_func, child_ancestor);

            Assert.IsTrue(result);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InternalIndirectTranslationTables()
        {
            var env = Environment.Create(new Options() { MiniEnvironment = true, DiscardingAnyExpressionDuringTests = true });
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
                NameFactory.ReferenceTypeReference(NameReference.Create(proxy_typename, NameReference.Create(parent_elemtype))),
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
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("i",
                            NameFactory.ReferenceTypeReference(NameReference.Create(proxy_typename, NameReference.Create(child_elemtype))),
                            FunctionCall.Create(NameReference.CreateThised("provide"))),
                        ExpressionFactory.Readout("i"),

                        assignment,
                        ExpressionFactory.Readout("e")
                        ))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorPassingReferenceAsTypeArgument()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement()));

            NameReference function_name = NameReference.Create("proxy",
                NameFactory.ReferenceTypeReference(NameFactory.Int64TypeReference()));

            root_ns.AddBuilder(FunctionBuilder.Create("tester",
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(
                    FunctionCall.Create(function_name)
                    )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceAsTypeArgument, function_name.TemplateArguments.Single()));

            return resolver;
        }


        [TestMethod]
        public IErrorReporter InferredPartialTemplateArgumentsOnConstraints()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("part",
                TemplateParametersBuffer.Create("T", "X").Values,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("X")
                    .BaseOf(NameReference.Create("T"))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("part", NameFactory.Int64TypeReference(),
                NameFactory.SinkReference()));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "caller",
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    call
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[0].Evaluation.Components);
            Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[1].Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InferredTemplateArgumentsOnConstraints()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create("part",
                TemplateParametersBuffer.Create("T", "X").Values,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), ExpressionReadMode.CannotBeRead))
                .Constraints(ConstraintBuilder.Create("X")
                    .BaseOf(NameReference.Create("T"))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("part"), Int64Literal.Create("5"));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                "caller",
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    call
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[0].Evaluation.Components);
            Assert.AreEqual(env.Int64Type.InstanceOf, call.Name.TemplateArguments[1].Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingConstConstraint()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Mut").SetModifier(EntityModifier.Mutable));

            NameReference parent_constraint = NameReference.Create("Mut");
            root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("T")
                    .SetModifier(EntityModifier.Const)
                    .Inherits(parent_constraint)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_constraint));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_constraint));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingTypesConstraint()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                .SetModifier(EntityModifier.Base));
            root_ns.AddBuilder(TypeBuilder.Create("Child").Parents("Parent"));

            NameReference baseof_name = NameReference.Create("Parent");
            NameReference parent_name = NameReference.Create("Child");
            root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("T")
                    .BaseOf(baseof_name)
                    .Inherits(parent_name)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConstraintConflictingTypeHierarchy, baseof_name));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorHasConstraint()
        {
            var env = Environment.Create(new Options() { AllowInvalidMainResult = true, AllowProtocols = true });
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration("getMe",
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference());

            // this function accepts any parameter where parameter type has function "getMe"
            FunctionDefinition constrained_func = root_ns.AddBuilder(FunctionBuilder.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values,
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameFactory.PointerTypeReference("T"))));

            // this type does NOT have function "getMe"
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("YMan")
                .With(FunctionBuilder.Create("missing",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y_man")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "main",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("y_man",null,ExpressionFactory.HeapConstructor(NameReference.Create("YMan"))),
                    Return.Create(call)
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedHasFunctionConstraint, call.Callee));

            return resolver;
        }

    }
}
