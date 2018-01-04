using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Templates
    {
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

            FunctionCall call = FunctionCall.Create(NameReference.Create("part",NameFactory.IntTypeReference(),NameReference.Sink()));
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
                TemplateParametersBuffer.Create("T","X").Values),
                NameFactory.UnitTypeReference(),

                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x",NameReference.Create("T"),ExpressionReadMode.CannotBeRead))
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