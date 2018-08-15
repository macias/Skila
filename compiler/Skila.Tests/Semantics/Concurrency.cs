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
    public class Concurrency : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorSpawningMutables()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), null,
                        EntityModifier.Public | env.Options.ReassignableModifier()))
                    .With(FunctionBuilder.Create("empty",
                            ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                            Block.CreateStatement())
                            .Parameters(FunctionParameter.Create("p", NameFactory.PointerNameReference(NameReference.Create("Point")), Variadic.None,
                            null, isNameRequired: false, usageMode: ExpressionReadMode.CannotBeRead))));

                FunctionArgument mutable_arg = FunctionArgument.Create(NameReference.Create("r"));
                NameReference mutable_method = NameReference.Create("r", "empty");
                var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("r",null, ExpressionFactory.HeapConstructor(NameReference.Create("Point"))),
                    Spawn.Create(FunctionCall.Create(mutable_method,mutable_arg)),
                    Return.Create(NameReference.Create("r","x"))
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotSpawnWithMutableArgument, mutable_arg));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotSpawnOnMutableContext, mutable_method));
            }

            return resolver;
        }
    }
}
