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
    public class MethodDefinitions
    {
        [TestMethod]
        public IErrorReporter Basics()
        {
            var env = Environment.Create(new Options() {}.DisableSingleMutability());
            var root_ns = env.Root;

            var func_def = FunctionBuilder.Create(
                "foo",
                ExpressionReadMode.OptionalUse,
                NameFactory.RealTypeReference(),
                Block.CreateStatement(new[] { Return.Create(RealLiteral.Create("3.3")) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo").With(func_def));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorVirtualCallInsideConstructor()
        {
            var env = Environment.Create(new Options() {}.DisableSingleMutability());
            var root_ns = env.Root;

            FunctionCall virtual_call = FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName, "foo"));
            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .SetModifier(EntityModifier.Base)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                    Block.CreateStatement(new[] {
                        virtual_call
                    })))
                .With(FunctionBuilder.Create("foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                        Return.Create(RealLiteral.Create("3.3"))
                    }))
                    .SetModifier(EntityModifier.Base)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VirtualCallFromConstructor, virtual_call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCallingConstructorFromBody()
        {
            var env = Environment.Create(new Options() {}.DisableSingleMutability());
            var root_ns = env.Root;

            FunctionCall constructor_call = FunctionCall.Create(NameReference.Create(NameFactory.ThisVariableName,
                NameFactory.InitConstructorName));
            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo")
                .SetModifier(EntityModifier.Base)
                .With(FunctionDefinition.CreateInitConstructor(EntityModifier.None, null,
                    Block.CreateStatement()))
                .With(FunctionBuilder.Create("foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.RealTypeReference(),
                    Block.CreateStatement(new IExpression[] {
                        constructor_call,
                        Return.Create(RealLiteral.Create("3.3"))
                    }))
                    .SetModifier(EntityModifier.Base)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ConstructorCallFromFunctionBody, constructor_call));

            return resolver;
        }
    }
}