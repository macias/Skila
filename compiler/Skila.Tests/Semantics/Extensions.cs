using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Extensions
    {
        [TestMethod]
        public IErrorReporter ErrorInvalidDefinitions()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Extension ext = root_ns.AddNode(Extension.Create());

                FunctionParameter second_this_param = FunctionParameter.Create("y", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference()),
                        EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("second_this", NameFactory.Nat8TypeReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("x", "y"))))
                    .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference())),
                        second_this_param));

                FunctionParameter opt_this_param = FunctionParameter.Create("a", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference()),
                    Variadic.None, Nat8Literal.Create("0"), false, EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("opt_this", NameFactory.Nat8TypeReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("a", "a"))))
                    .Parameters(opt_this_param));

                FunctionParameter variadic_this_param = FunctionParameter.Create("b", NameFactory.ReferenceTypeReference(NameFactory.Nat8TypeReference()),
                    Variadic.Create(2, 3), null, false, EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("variadic_this", NameFactory.SizeTypeReference(), Block.CreateStatement(
                    Return.Create(FunctionCall.Create(NameReference.Create("b", NameFactory.IIterableCount)))))
                    .Parameters(variadic_this_param)
                    .Include(NameFactory.LinqExtensionReference()));

                FunctionParameter value_this_param = FunctionParameter.Create("c", NameFactory.Nat8TypeReference(), EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("value_this", NameFactory.Nat8TypeReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("c", "c"))))
                    .Parameters(value_this_param));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonPrimaryThisParameter, second_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.OptionalThisParameter, opt_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariadicThisParameter, variadic_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonReferenceThisParameter, value_this_param));
            }

            return resolver;
        }

    }
}
