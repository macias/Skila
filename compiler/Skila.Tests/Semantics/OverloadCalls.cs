using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Builders;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class OverloadCalls
    {
        [TestMethod]
        public IErrorReporter PreferringNonVariadicFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                FunctionDefinition func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(),
                        Variadic.Create(), null, false, usageMode: ExpressionReadMode.CannotBeRead)));
                FunctionDefinition func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()));
                var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealTypeReference(),
                    call, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def2, call.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DistinctTypesOverloadCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;
                var system_ns = env.SystemNamespace;

                var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.RealTypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()));
                root_ns.AddNode(VariableDeclaration.CreateStatement("s", NameFactory.RealTypeReference(), Undef.Create()));
                var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("s")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealTypeReference(),
                    call1, modifier: EntityModifier.Public));
                root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.RealTypeReference(),
                    call2, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
                Assert.AreEqual(func_def2, call2.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DistinctRequiredNamesOverloadCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, true,
                        usageMode: ExpressionReadMode.CannotBeRead)));
                var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("y", NameFactory.Int64TypeReference(), Variadic.None, null, true,
                        usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()));
                var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create("x", NameReference.Create("i")));
                var call2 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create("y", NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealTypeReference(),
                    call1, modifier: EntityModifier.Public));
                root_ns.AddNode(VariableDeclaration.CreateStatement("y", NameFactory.RealTypeReference(),
                    call2, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
                Assert.AreEqual(func_def2, call2.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplatedSpecializedOverloadCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", "T", VarianceMode.None,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create(),
                    modifier: EntityModifier.Public));
                var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealTypeReference(),
                    call1, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter InheritanceSpecializedOverloadCall()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var func_def1 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                var func_def2 = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")) }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.IObjectTypeReference(), usageMode: ExpressionReadMode.CannotBeRead)));
                root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create(),
                    modifier: EntityModifier.Public));
                var call1 = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.RealTypeReference(),
                    call1, modifier: EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(func_def1, call1.Resolution.TargetFunctionInstance.Target);
            }

            return resolver;
        }

    }
}