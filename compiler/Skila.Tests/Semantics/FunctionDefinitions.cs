using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class FunctionDefinitions
    {
        [TestMethod]
        public IErrorReporter ProperReturning()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("5")) })));
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foox"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { Return.Create() })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorReturning()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new[] { Return.Create() })));
            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foox"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] { Return.Create(IntLiteral.Create("5")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count());
            Assert.IsTrue(resolver.ErrorManager.Errors.All(it => it.Code == ErrorCode.TypeMismatch));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter AnonymousVariadicParameters()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, isNameRequired: false);
            // the tail variadic parameter has to require name
            var param2 = FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.Create(0, null), null, isNameRequired: false);
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { param1, param2 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), null));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.AnonymousTailVariadicParameter, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(param2, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter VariadicParametersInvalidLimits()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var param1 = FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(4, 3), null, isNameRequired: false);
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { param1 },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), null));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InvalidVariadicLimits, param1));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter NonConflictingOverload()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
               NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
               ExpressionReadMode.OptionalUse,
               NameFactory.DoubleTypeReference(),
               Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddBuilder(FunctionBuilder.Create(
               NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.DoubleTypeReference(), Variadic.None, null, false) },
               ExpressionReadMode.OptionalUse,
               NameFactory.DoubleTypeReference(),
               Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingVariadicOverload()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.Create(1,2),
                    null,isNameRequired: false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x",NameFactory.IntTypeReference(),  Variadic.Create(3,4),
                    null, isNameRequired: false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorConflictingOverlappingOptionalOverload()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false)
                },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddBuilder(FunctionBuilder.Create(
               NameDefinition.Create("foo"), new[] {
                   FunctionParameter.Create("x",NameFactory.IntTypeReference(),  Variadic.None,IntLiteral.Create("3"), false)
               },
               ExpressionReadMode.OptionalUse,
               NameFactory.DoubleTypeReference(),
               Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDefaultUndef()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            Undef default_value = Undef.Create();
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x",NameFactory.IntTypeReference(),  Variadic.None,default_value, false)
                },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InitializationWithUndef, default_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConflictingAdditionalOptionalOverload()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(),
                Variadic.None, IntLiteral.Create("3"), false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.OverloadingDuplicateFunctionDefinition, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateSpecializationOverload()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo", "T", VarianceMode.None), new[] { FunctionParameter.Create("x", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddBuilder(FunctionBuilder.Create(
               NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
               NameFactory.DoubleTypeReference(),
               Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }
    }
}