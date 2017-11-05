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
    public class Protocols
    {
        [TestMethod]
        public IErrorReporter ErrorCallingConstructor()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .Modifier(EntityModifier.Protocol));

            NameReference typename = NameReference.Create("IX");
            NameReference cons_ref;
            root_ns.AddNode(FunctionDefinition.CreateFunction(EntityModifier.None,
                NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x",NameReference.Create("IX"),
                        ExpressionFactory.StackConstructorCall(typename,out cons_ref)),
                    Tools.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            // todo: currently the error is too generic and it is reported for hidden node
            // translate this to meaningful error and for typename
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound,cons_ref));

            return resolver;
        }

    }
}