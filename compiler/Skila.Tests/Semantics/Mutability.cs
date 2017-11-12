using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Mutability
    {
        [TestMethod]
        public IErrorReporter ErrorImmutableTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar"));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.IntTypeReference(),
                null, EntityModifier.Reassignable);
            VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                Undef.Create());
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point", "T")
               .Modifier(EntityModifier.Const)
               .With(decl1)
               .With(decl2));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReassignableFieldInImmutableType, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFieldInImmutableType, decl2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorViolatingConstConstraint()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar"));
            root_ns.AddBuilder(TypeBuilder.Create("Foo").Modifier(EntityModifier.Const));

            VariableDeclaration field = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                Undef.Create());
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point",
                    TemplateParametersBuffer.Create().Add("T").With(EntityModifier.Const).Values))
               .Modifier(EntityModifier.Const)
               .With(field));

            NameReference wrong_type = NameReference.Create("Point", NameReference.Create("Bar"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameReference.Create("Point",NameReference.Create("Foo")), Undef.Create()),
                    VariableDeclaration.CreateStatement("y", wrong_type, Undef.Create()),
                    Tools.Readout("x"),
                    Tools.Readout("y"),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedConstConstraint, wrong_type));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableGlobalVariables()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar"));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.IntTypeReference(),
                null, EntityModifier.Reassignable);
            VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("Bar"),
                null);

            root_ns.AddNode(decl1);
            root_ns.AddNode(decl2);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalReassignableVariable, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalMutableVariable, decl2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMixedInheritance()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Parent").Modifier(EntityModifier.Base));
            TypeDefinition child_type = root_ns.AddBuilder(TypeBuilder.Create("Child").Parents("Parent").Modifier(EntityModifier.Const));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ImmutableInheritsMutable, child_type));

            return resolver;
        }
    }
}
