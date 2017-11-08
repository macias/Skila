using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Expressions;
using Skila.Language.Flow;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Inheritance
    {
        [TestMethod]
        public IErrorReporter ErrorIncorrectMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionDefinition.CreateDeclaration(EntityModifier.None,
                    NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference()))
                .With(FunctionDefinition.CreateDeclaration(EntityModifier.None,
                    NameDefinition.Create("bar"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference()))
                .Modifier(EntityModifier.Protocol));

            FunctionDefinition func_impl = FunctionDefinition.CreateFunction(EntityModifier.None,
                    NameDefinition.Create("bar"), Enumerable.Empty<FunctionParameter>(),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }));
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(func_impl)
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingFunctionImplementation, type_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingDerivedModifier, func_impl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperBasicMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionDefinition.CreateDeclaration(EntityModifier.None,
                    NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired:false)},
                    ExpressionReadMode.OptionalUse,
                    NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference())))
                .Modifier(EntityModifier.Protocol));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(FunctionDefinition.CreateFunction(EntityModifier.Derived,
                    NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.OptionalUse,
                    // subtype of original result typename -- this is legal
                    NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                    Block.CreateStatement(new[] {
                        Return.Create(ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(), IntLiteral.Create("2")))
                    })))
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}