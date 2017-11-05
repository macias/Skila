using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Exceptions
    {
        [TestMethod]
        public IErrorReporter ErrorThrowingNonException()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            IntLiteral throw_value = IntLiteral.Create("3");
            root_ns.AddNode(Throw.Create(throw_value));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, throw_value));

            return resolver;
        }    
    }
}
