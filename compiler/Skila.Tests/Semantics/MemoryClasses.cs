using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class MemoryClasses
    {
        [TestMethod]
        public IErrorReporter ErrorPersistentReferenceType()
        {
            var env = Language.Environment.Create(new Options() { AllowDiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl1 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create());
            root_ns.AddNode(decl1);

            var decl2 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create(), modifier: EntityModifier.Static);

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl2,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorHeapTypeOnStack()
        {
            var env = Language.Environment.Create(new Options() { AllowDiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl = VariableDeclaration.CreateStatement("bar", NameFactory.StringTypeReference(),
                initValue: StringLiteral.Create("hi"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapTypeOnStack, decl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversion()
        {
            var env = Language.Environment.Create(new Options() { AllowDiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.IntTypeReference(), initValue: IntLiteral.Create("3"));
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }


        [TestMethod]
        public IErrorReporter ImplicitPointerReferenceConversion()
        {
            var env = Language.Environment.Create(new Options() { AllowDiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl_src = VariableDeclaration.CreateStatement("foo", NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                initValue: Undef.Create());
            var decl_dst = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                initValue: NameReference.Create("foo"));

            var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    decl_src,
                    decl_dst,
                    ExpressionFactory.Readout("bar")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ImplicitValueReferenceConversionOnCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    FunctionCall.Create(NameReference.Create("foo"),FunctionArgument.Create( IntLiteral.Create("5")))
                })));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.CannotBeRead,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement())
                .Parameters(FunctionParameter.Create("x", NameFactory.ReferenceTypeReference(NameFactory.IntTypeReference()),
                    usageMode: ExpressionReadMode.CannotBeRead)));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}