using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class NameResolution
    {
        [TestMethod]
        public IErrorReporter ErrorReadingBeforeDefinition()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var x_ref = NameReference.Create("x");
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foox"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("a", NameFactory.IntTypeReference(), x_ref),
                    VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("1")),
                    Tools.Readout("a"),
                    Tools.Readout("x")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(x_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorCircularReference()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var x_ref = NameReference.Create("x");
            var decl = VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), x_ref);

            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.CircularReference, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(decl, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorDuplicatedName()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("1")));
            var second_decl = root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.IntTypeReference(), IntLiteral.Create("2")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.NameAlreadyExists, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(second_decl, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingQualifiedReferenceToNestedTarget()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            // reference to nested target
            var string_ref = root_ns.AddNode(NameReference.Create(NameFactory.SystemNamespaceReference(), NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingQualifiedReferenceInSameNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            // reference to the target in the same namespace
            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.SystemNamespaceReference(), NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorResolvingUnqualifiedReferenceToNestedNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            // incorrect, wrong namespace
            var string_ref = root_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, string_ref.Binding.Matches.Count());
            Assert.IsFalse(string_ref.Binding.HasMatch);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingUnqualifiedReferenceWithinSameNamespace()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, string_ref.Binding.Matches.Count());
            Assert.AreEqual(env.StringType, string_ref.Binding.Match.Target);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ResolvingForDuplicatedType()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var dup_type = TypeBuilder.Create(NameDefinition.Create(NameFactory.StringTypeName)).Build();
            system_ns.AddNode(dup_type);

            var string_ref = system_ns.AddNode(NameReference.Create(NameFactory.StringTypeName));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, string_ref.Binding.Matches.Count());
            Assert.IsTrue(string_ref.Binding.Matches.Any(it => it.Target == env.StringType));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TemplateResolving()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var tuple_ref = NameReference.Create("Tuple", NameReference.Create("T"));

            var tuple_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Tuple", "T", VarianceMode.None))
                .With(tuple_ref));
            var abc_type = system_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("ABC")));
            var derived_type = system_ns.AddBuilder(TypeBuilder.Create("Deriv").Parents(NameReference.Create("ABC")));

            var tuple_abc_ref = system_ns.AddNode(NameReference.Create("Tuple", NameReference.Create("ABC")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, tuple_ref.Binding.Matches.Count());
            Assert.AreEqual(tuple_type, tuple_ref.Binding.Match.Target);
            Assert.AreEqual(tuple_type.NestedTypes.Single(),
                tuple_ref.Binding.Match.TemplateArguments.Single().Target());

            Assert.AreEqual(1, tuple_abc_ref.Binding.Matches.Count());
            Assert.AreEqual(tuple_type, tuple_abc_ref.Binding.Match.Target);
            Assert.AreEqual(abc_type, tuple_abc_ref.Binding.Match.TemplateArguments.Single().Target());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter FunctionTypes()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            FunctionDefinition func_def = FunctionBuilder.Create(
                NameDefinition.Create("sqrt"), new[] { FunctionParameter.Create("r", NameReference.Create("Double"), Variadic.None, null, false) },
                ExpressionReadMode.OptionalUse,
                NameReference.Create("Double"),
                Block.CreateStatement(Enumerable.Empty<IExpression>()));
            root_ns.AddNode(func_def);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, func_def.TypeName.Binding.Matches.Count);
            Assert.AreEqual(resolver.Context.Env.FunctionTypes[1], func_def.TypeName.Binding.Match.Target);

            return resolver;
        }

    }
}
