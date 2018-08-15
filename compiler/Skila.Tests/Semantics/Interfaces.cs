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
    public class Interfaces : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorCallingConstructor()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("IX")
                    .SetModifier(EntityModifier.Interface));

                NameReference typename = NameReference.Create("IX");
                NameReference cons_ref;
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x",NameReference.Create("IX"),
                         ExpressionFactory.StackConstructor(typename,out cons_ref)),
                     ExpressionFactory.Readout("x")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                // todo: currently the error is too generic and it is reported for hidden node
                // translate this to meaningful error and for typename
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, cons_ref));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter DuckTypingInterfaces()
        {
            return duckTyping(new Options()
            {
                InterfaceDuckTyping = true,
                DiscardingAnyExpressionDuringTests = true,
                AllowInvalidMainResult = true,
                DebugThrowOnError = true
            });
        }
        [TestMethod]
        public IErrorReporter DuckTypingProtocols()
        {
            return duckTyping(new Options()
            {
                InterfaceDuckTyping = false,
                DiscardingAnyExpressionDuringTests = true,
                AllowInvalidMainResult = true,
                DebugThrowOnError = true,
                AllowProtocols = true,
            });
        }

        private IErrorReporter duckTyping(Options options)
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(options.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("IX")
                     .With(FunctionBuilder.CreateDeclaration(
                         "bar",
                         ExpressionReadMode.OptionalUse,
                         NameFactory.PointerNameReference(NameFactory.IObjectNameReference()))
                         .Parameters(FunctionParameter.Create("x", NameFactory.BoolNameReference(), Variadic.None, null, isNameRequired: false)))
                     .SetModifier(env.Options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol));

                root_ns.AddBuilder(TypeBuilder.Create("X")
                    .With(FunctionBuilder.Create("bar",
                        ExpressionReadMode.OptionalUse,
                        // subtype of original result typename -- this is legal
                        NameFactory.PointerNameReference(NameFactory.Int64NameReference()),
                        Block.CreateStatement(new[] {
                        Return.Create( ExpressionFactory.HeapConstructor(NameFactory.Int64NameReference(), Int64Literal.Create("2")))
                        }))
                        .Parameters(FunctionParameter.Create("x", NameFactory.BoolNameReference(), usageMode: ExpressionReadMode.CannotBeRead))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerNameReference(NameReference.Create("IX")),null,env.Options.ReassignableModifier()),
                    Assignment.CreateStatement(NameReference.Create("i"), ExpressionFactory.HeapConstructor(NameReference.Create("X"))),
                     ExpressionFactory.Readout("i"),
                    Return.Create(Int64Literal.Create("2"))
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDuckTypingInterfaceValues()
        {
            return errorDuckTypingValues(new Options()
            {
                InterfaceDuckTyping = true,
                DiscardingAnyExpressionDuringTests = true,
                AllowInvalidMainResult = true
            });
        }

        [TestMethod]
        public IErrorReporter ErrorDuckTypingProtocolValues()
        {
            return errorDuckTypingValues(new Options()
            {
                AllowProtocols = true,
                InterfaceDuckTyping = false,
                DiscardingAnyExpressionDuringTests = true,
                AllowInvalidMainResult = true
            });
        }

        private IErrorReporter errorDuckTypingValues(Options options)
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(options.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("IX")
                     .SetModifier(env.Options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol));

                root_ns.AddBuilder(TypeBuilder.Create("X"));

                IExpression init_value =  ExpressionFactory.StackConstructor(NameReference.Create("X"));
                // even with duck typing we cannot make the assigment because slicing is forbidden in all cases
                VariableDeclaration decl = VariableDeclaration.CreateStatement("i", NameReference.Create("IX"), init_value);
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    decl,
                     ExpressionFactory.Readout("i"),
                    Return.Create(Int64Literal.Create("2"))
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));
            }

            return resolver;
        }
    }
}