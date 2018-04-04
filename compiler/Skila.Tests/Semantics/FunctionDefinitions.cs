using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionDefinitions
    {
        [TestMethod]
        public IErrorReporter ErrorInvalidMainResultType()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            NameReference result_typename = NameFactory.UnitTypeReference();
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameFactory.MainFunctionName,
                ExpressionReadMode.OptionalUse,
                result_typename,

                Block.CreateStatement()));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MainFunctionInvalidResultType,result_typename));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInvalidConverters()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            FunctionDefinition conv1 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                 NameFactory.Int64TypeReference(),
                    Block.CreateStatement(Return.Create(Int64Literal.Create("3"))));
            FunctionDefinition conv2 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                 ExpressionReadMode.OptionalUse,
                 NameFactory.BoolTypeReference(),
                    Block.CreateStatement(Return.Create(Undef.Create())))
                    .SetModifier(EntityModifier.Pinned);
            FunctionDefinition conv3 = FunctionBuilder.Create(NameFactory.ConvertFunctionName,
                 NameFactory.StringPointerTypeReference(),
                    Block.CreateStatement(Return.Create(Undef.Create())))
                    .SetModifier(EntityModifier.Pinned)
                    .Parameters(FunctionParameter.Create("x",NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead));

            root_ns.AddBuilder(TypeBuilder.Create("Start")
                .SetModifier(EntityModifier.Base)
                .With(conv1)
                .With(conv2)
                .With(conv3));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterNotPinned, conv1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterDeclaredWithIgnoredOutput, conv2));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConverterWithParameters, conv3));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCannotInferResultType()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            IExpression lambda = FunctionBuilder.CreateLambda(null,
                    Block.CreateStatement(new IExpression[] {
                        IfBranch.CreateIf(BoolLiteral.CreateFalse(),new[]{
                            Return.Create(BoolLiteral.CreateTrue())
                        }),
                        Return.Create(Int64Literal.Create("2"))
                    })).Build();
            root_ns.AddBuilder(FunctionBuilder.Create("me",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    // f = () => x
                    VariableDeclaration.CreateStatement("f",null,lambda),
                    ExpressionFactory.Readout("f")
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotInferResultType, lambda));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperReturning()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("5")) })));
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                "foox",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] { Return.Create() })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorReturning()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            Return empty_return = Return.Create();
            var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] { empty_return })));
            Int64Literal return_value = Int64Literal.Create("5");
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                "foox",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] { Return.Create(return_value) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EmptyReturn,empty_return));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, return_value));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AnonymousVariadicParameters()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.Create(), null,
                isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            // the tail variadic parameter has to require name
            var param2 = FunctionParameter.Create("y", NameFactory.Int64TypeReference(), Variadic.Create(), null,
                isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo", 
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                })).Parameters(param1, param2));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), null, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AnonymousTailVariadicParameter, param2));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorVariadicParametersInvalidLimits()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true });
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.Create(4, 3), null,
                isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead);
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3"))
                }))
                .Parameters(param1));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), null, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidVariadicLimits, param1));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter NonConflictingOverload()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
               "foo",
               ExpressionReadMode.OptionalUse,
               NameFactory.RealTypeReference(),
               Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                   .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                    usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddBuilder(FunctionBuilder.Create(
               "foo",
               ExpressionReadMode.OptionalUse,
               NameFactory.RealTypeReference(),
               Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                   .Parameters(FunctionParameter.Create("x", NameFactory.RealTypeReference(), Variadic.None, null, false,
                    usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingVariadicOverload()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo", 
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.Create(1, 3),
                    null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.Create(3, 5),
                    null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorConflictingOverlappingOptionalOverload()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddBuilder(FunctionBuilder.Create(
               "foo", 
               ExpressionReadMode.OptionalUse,
               NameFactory.RealTypeReference(),
               Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                   .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, Int64Literal.Create("3"), false,
                        usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDefaultUndef()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            Undef default_value = Undef.Create();
            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, default_value, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InitializationWithUndef, default_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingAdditionalOptionalOverload()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) }))                );
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                "foo", 
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) })).Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(),
                    Variadic.None, Int64Literal.Create("3"), false, usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateSpecializationOverload()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                "foo", "T", VarianceMode.None, 
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
            root_ns.AddBuilder(FunctionBuilder.Create(
               "foo",
                ExpressionReadMode.OptionalUse,
               NameFactory.RealTypeReference(),
               Block.CreateStatement(new[] {
                   Return.Create(RealLiteral.Create("3.3")) }))
                   .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringParameters()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            FunctionParameter parameter = FunctionParameter.Create("x", NameFactory.Int64TypeReference());
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                ExpressionReadMode.ReadRequired,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) }))
                .Parameters(parameter));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.BindableNotUsed, parameter.Name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingDisabledParameters()
        {
            var env = Environment.Create(new Options() {});
            var root_ns = env.Root;

            NameReference param_ref = NameReference.Create("x");
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                ExpressionReadMode.ReadRequired,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(param_ref)
                }))
                .Parameters(FunctionParameter.Create("x", NameFactory.RealTypeReference(), ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, param_ref));

            return resolver;
        }
    }
}