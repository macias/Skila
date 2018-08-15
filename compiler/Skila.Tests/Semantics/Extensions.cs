using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;
using System.Linq;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Extensions : ITest
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

                // `this` parameter as the second one --> error
                FunctionParameter second_this_param = FunctionParameter.Create("y",
                    NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference()), EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("second_this", NameFactory.Nat8NameReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("x", "y"))))
                    .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference())),
                        second_this_param));

                // `this` parameter as optional one --> error
                FunctionParameter opt_this_param = FunctionParameter.Create("a",
                    NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference()), Variadic.None,
                        Nat8Literal.Create("0"), false, EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("opt_this", NameFactory.Nat8NameReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("a", "a"))))
                    .Parameters(opt_this_param));

                // variadic `this` parameter --> error
                FunctionParameter variadic_this_param = FunctionParameter.Create("b",
                    NameFactory.ReferenceNameReference(NameFactory.Nat8NameReference()), 
                    Variadic.Create(2, 3), null, false, EntityModifier.This);
                FunctionCall b_count = FunctionCall.Create(NameReference.Create("b", NameFactory.IIterableCount));
                ext.AddBuilder(FunctionBuilder.Create("variadic_this", NameFactory.SizeNameReference(),
                    Block.CreateStatement(
                        // this is invalid as well, because it would mean we array of references and we try to make
                        // a template function (`count`) executed with reference as its template argument
                        // return b.count()
                        Return.Create(b_count)))
                    .Parameters(variadic_this_param)
                    .Include(NameFactory.LinqExtensionReference()));

                // `this` parameter as value (no reference) --> error
                FunctionParameter value_this_param = FunctionParameter.Create("c", NameFactory.Nat8NameReference(), EntityModifier.This);
                ext.AddBuilder(FunctionBuilder.Create("value_this", NameFactory.Nat8NameReference(), Block.CreateStatement(
                    Return.Create(ExpressionFactory.Mul("c", "c"))))
                    .Parameters(value_this_param));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(5, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonPrimaryThisParameter, second_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.OptionalThisParameter, opt_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariadicThisParameter, variadic_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NonReferenceThisParameter, value_this_param));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceAsTypeArgument, b_count.Name.TemplateArguments.Single()));
            }

            return resolver;
        }

    }
}
