using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Builders;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class OverloadCalls
    {
        [TestMethod]
        public IErrorReporter PreferringNonVariadicFunction()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            FunctionDefinition func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), 
                    Variadic.Create(0,null), null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            FunctionDefinition func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), 
                    Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def2, call.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DistinctTypesOverloadCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.DoubleTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            root_ns.AddNode(VariableDeclaration.CreateStatement("s", NameFactory.DoubleTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("s")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(),
                call2));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
            Assert.AreEqual(func_def2, call2.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DistinctRequiredNamesOverloadCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, true) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("y", NameFactory.IntTypeReference(), Variadic.None, null, true) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create("x",NameReference.Create("i")));
            var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create("y",NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1));
            root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.DoubleTypeReference(),
                call2));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
            Assert.AreEqual(func_def2, call2.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplatedSpecializedOverloadCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo","T",VarianceMode.None), new[] { FunctionParameter.Create("x", NameReference.Create("T"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InheritanceSpecializedOverloadCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.IntTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] { FunctionParameter.Create("x", NameFactory.ObjectTypeReference(), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(), 
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) })));
            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.IntTypeReference(), Undef.Create()));
            var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.DoubleTypeReference(),
                call1));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);

            return resolver;
        }

    }
}