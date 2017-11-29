using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Interpreter;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Concurrency
    {
        [TestMethod]
        public IErrorReporter ErrorSpawningMutables()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), null, 
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(FunctionBuilder.Create(NameDefinition.Create("empty"), new[] {
                    FunctionParameter.Create("p",NameFactory.PointerTypeReference(NameReference.Create("Point")),Variadic.None,
                        null,isNameRequired:false)}, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(),
                        Block.CreateStatement(new[] {
                            ExpressionFactory.Readout("p")
                        }))));

            FunctionArgument mutable_arg = FunctionArgument.Create(NameReference.Create("r"));
            NameReference mutable_method = NameReference.Create("r", "empty");
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("r",null,ExpressionFactory.HeapConstructorCall(NameReference.Create("Point"))),
                    Spawn.Create(FunctionCall.Create(mutable_method,mutable_arg)),
                    Return.Create(NameReference.Create("r","x"))
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotSpawnWithMutableArgument, mutable_arg));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotSpawnOnMutableContext, mutable_method));

            return resolver;
        }
    }
}
