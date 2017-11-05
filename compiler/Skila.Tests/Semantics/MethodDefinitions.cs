using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class MethodDefinitions
    {
        [TestMethod]
        public IErrorReporter Basics()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] { Return.Create(DoubleLiteral.Create("3.3")) }));

            var type_def = root_ns.AddBuilder(TypeBuilder.Create("Foo").With(func_def));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }

    }
}