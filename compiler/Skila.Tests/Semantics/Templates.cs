using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System.Linq;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Templates
    {
        [TestMethod]
        public IErrorReporter TranslationTableOfInferredCommonTypes()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            TemplateParameter template_param = TemplateParametersBuffer.Create("T").Values.Single();
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("getMe", new[] { template_param }),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(), Block.CreateStatement())
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T"), ExpressionReadMode.CannotBeRead),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"), ExpressionReadMode.CannotBeRead)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("getMe"), NameReference.Create("x"), NameReference.Create("y"));
            root_ns.AddBuilder(FunctionBuilder.Create("common",
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                            IntLiteral.Create("3")),
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
            Assert.AreEqual(resolver.Context.Env.ObjectType.InstanceOf, common_instance);

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
                .Modifier(EntityModifier.Abstract)
                .With(base_func));

            const string child_typename = "Kid";
            const string child_elemtype = "CT";

            FunctionDefinition deriv_func = FunctionBuilder.Create("getMe",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.ReferenceTypeReference(child_elemtype),
                    Block.CreateStatement(Return.Create(Undef.Create())))
                    .Modifier(EntityModifier.Override);
            TypeDefinition child = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create(child_typename,
                    TemplateParametersBuffer.Create(child_elemtype).Values))
                .Parents(NameReference.Create(parent_typename, NameReference.Create(child_elemtype)))
                .With(deriv_func));

            var resolver = NameResolver.Create(env);

            // testing here template translation
            EntityInstance child_ancestor = child.Inheritance.AncestorsWithoutObject.Single();
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
                .Modifier(EntityModifier.Base)
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
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement()));

            NameReference function_name = NameReference.Create("proxy",
                NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()));

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
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("part",
                TemplateParametersBuffer.Create("T", "X").Values),
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("X")
                    .BaseOf(NameReference.Create("T"))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("part", NameFactory.IntTypeReference(), NameReference.Sink()));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("caller"),
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    call
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.IntType.InstanceOf, call.Name.TemplateArguments[0].Evaluation.Components);
            Assert.AreEqual(env.IntType.InstanceOf, call.Name.TemplateArguments[1].Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InferredTemplateArgumentsOnConstraints()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("part",
                TemplateParametersBuffer.Create("T", "X").Values),
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), ExpressionReadMode.CannotBeRead))
                .Constraints(ConstraintBuilder.Create("X")
                    .BaseOf(NameReference.Create("T"))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("part"), IntLiteral.Create("5"));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("caller"),
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    call
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(env.IntType.InstanceOf, call.Name.TemplateArguments[0].Evaluation.Components);
            Assert.AreEqual(env.IntType.InstanceOf, call.Name.TemplateArguments[1].Evaluation.Components);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingConstConstraint()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Mut").Modifier(EntityModifier.Mutable));

            NameReference parent_constraint = NameReference.Create("Mut");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Constraints(ConstraintBuilder.Create("T")
                    .Modifier(EntityModifier.Const)
                    .Inherits(parent_constraint)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ImmutableInheritsMutable, parent_constraint));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_constraint));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorConflictingTypesConstraint()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                .Modifier(EntityModifier.Base));
            root_ns.AddBuilder(TypeBuilder.Create("Child").Parents("Parent"));

            NameReference baseof_name = NameReference.Create("Parent");
            NameReference parent_name = NameReference.Create("Child");
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
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
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition func_constraint = FunctionBuilder.CreateDeclaration(NameDefinition.Create("getMe"),
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference());

            // this function accepts any parameter where parameter type has function "getMe"
            FunctionDefinition constrained_func = root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("proxy",
                TemplateParametersBuffer.Create().Add("T").Values),
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                         Return.Create(FunctionCall.Create(NameReference.Create("t","getMe")))
                     }))
                     .Constraints(ConstraintBuilder.Create("T").Has(func_constraint))
                     .Parameters(FunctionParameter.Create("t", NameFactory.PointerTypeReference("T"))));

            // this type does NOT have function "getMe"
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("YMan")
                .With(FunctionBuilder.Create(NameDefinition.Create("missing"),
                    ExpressionReadMode.ReadRequired,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }))));

            FunctionCall call = FunctionCall.Create(NameReference.Create("proxy"), FunctionArgument.Create(NameReference.Create("y_man")));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
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