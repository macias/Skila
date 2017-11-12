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
    public class Interfaces
    {    
        [TestMethod]
        public IErrorReporter ErrorCallingConstructor()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .Modifier(EntityModifier.Interface));

            NameReference typename = NameReference.Create("IX");
            NameReference cons_ref;
            root_ns.AddBuilder(FunctionBuilder.Create(
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
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceNotFound, cons_ref));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter DuckTypingInterfaces()
        {
            return duckTyping(new Options() { InterfaceDuckTyping = true });
        }
        [TestMethod]
        public IErrorReporter DuckTypingProtocols()
        {
            return duckTyping(new Options() { InterfaceDuckTyping = false });
        }

        private IErrorReporter duckTyping(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                 .With(FunctionBuilder.CreateDeclaration(
                     NameDefinition.Create("bar"),
                     ExpressionReadMode.OptionalUse,
                     NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()))
                     .Parameters(FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false)))
                 .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol));

            root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.OptionalUse,
                    // subtype of original result typename -- this is legal
                    NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                    Block.CreateStatement(new[] {
                        Return.Create(ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(), IntLiteral.Create("2")))
                    }))));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i",NameFactory.PointerTypeReference(NameReference.Create("IX")),null,EntityModifier.Reassignable),
                    Assignment.CreateStatement(NameReference.Create("i"),ExpressionFactory.HeapConstructorCall(NameReference.Create("X"))),
                    Tools.Readout("i"),
                    Return.Create(IntLiteral.Create("2"))
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDuckTypingInterfaceValues()
        {
            return errorDuckTypingValues(new Options() { InterfaceDuckTyping = true });
        }

        [TestMethod]
        public IErrorReporter ErrorDuckTypingProtocolValues()
        {
            return errorDuckTypingValues(new Options() { InterfaceDuckTyping = false });
        }

        private IErrorReporter errorDuckTypingValues(IOptions options)
        {
            var env = Environment.Create(options);
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                 .Modifier(options.InterfaceDuckTyping ? EntityModifier.Interface : EntityModifier.Protocol));

            root_ns.AddBuilder(TypeBuilder.Create("X"));

            IExpression init_value = ExpressionFactory.StackConstructorCall(NameReference.Create("X"));
            var main_func = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.IntTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("i", NameReference.Create("IX"), init_value),
                    Tools.Readout("i"),
                    Return.Create(IntLiteral.Create("2"))
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));

            return resolver;
        }
    }
}