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
        public IErrorReporter ErrorNonVirtualInterfacePattern()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ITransmogrifier")
                .With(FunctionBuilder.CreateDeclaration("transmogrify", ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference())
                    .Modifier(EntityModifier.Private))
                .With(FunctionBuilder.CreateDeclaration("untransmogrify", ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference())
                    .Modifier(EntityModifier.Private))
            );

            NameReference private_reference = NameReference.Create("transmogrify");
            // refining private is OK, but we cannot change the access to it
            FunctionDefinition public_func = FunctionBuilder.Create("untransmogrify", ExpressionReadMode.CannotBeRead,
                    NameFactory.VoidTypeReference(), Block.CreateStatement())
                .Modifier(EntityModifier.Refines);

            root_ns.AddBuilder(TypeBuilder.Create("CardboardBox")
                .Parents("ITransmogrifier")
                // refining private is OK
                .With(FunctionBuilder.Create("transmogrify", ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Refines | EntityModifier.Private))
                .With(public_func)
                // but using it -- not
                .With(FunctionBuilder.Create("trying", ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(),
                    Block.CreateStatement(new[] {
                        FunctionCall.Create(private_reference)
                    })))
                );

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AccessForbidden, private_reference));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteredAccessLevel, public_func.Modifier));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNothingToDerive()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition function = FunctionBuilder.Create("getSome",
                ExpressionReadMode.ReadRequired, NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("3"))
                    }))
                    .Modifier(EntityModifier.Refines);
            root_ns.AddBuilder(TypeBuilder.Create("GetPos")
                .With(function));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToDerive, function));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInheritingHeapOnlyType()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                .Modifier(EntityModifier.HeapOnly | EntityModifier.Base));

            NameReference parent_name = NameReference.Create("Point");
            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("PointEx"))
                .Parents(parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CrossInheritingHeapOnlyType, parent_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInheritingFinalType()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point")));

            NameReference parent_name = NameReference.Create("Point");
            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("PointEx"))
                .Parents(parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritingSealedType, parent_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTypeImplementationAsSecondaryParent()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point1")).Modifier(EntityModifier.Base));
            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point2")).Modifier(EntityModifier.Base));

            NameReference parent_name = NameReference.Create("Point2");
            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("PointEx"))
                .Parents(NameReference.Create("Point1"), parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeImplementationAsSecondaryParent, parent_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter LowestCommonAncestor()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var abc_type = root_ns.AddBuilder(TypeBuilder.Create("ABC"));
            var foo_type = root_ns.AddBuilder(TypeBuilder.Create("Foo").Parents(NameReference.Create("ABC")));
            var bar_type = root_ns.AddBuilder(TypeBuilder.Create("Bar").Parents(NameReference.Create("ABC")));
            var deriv_type = root_ns.AddBuilder(TypeBuilder.Create("Deriv").Parents(NameReference.Create("Foo")));

            var deriv_ref = root_ns.AddNode(NameReference.Create("Deriv"));
            var bar_ref = root_ns.AddNode(NameReference.Create("Bar"));
            var abc_ref = root_ns.AddNode(NameReference.Create("ABC"));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(bar_type, bar_ref.Binding.Match.Target);
            Assert.AreEqual(deriv_type, deriv_ref.Binding.Match.Target);
            Assert.AreEqual(abc_type, abc_ref.Binding.Match.Target);

            bool found = TypeMatcher.LowestCommonAncestor(resolver.Context,
                bar_ref.Binding.Match, deriv_ref.Binding.Match, out IEntityInstance common);
            Assert.IsTrue(found);
            Assert.AreEqual(abc_ref.Binding.Match, common);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter LowestCommonAncestorDoubleInt()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var resolver = NameResolver.Create(env);

            bool found = TypeMatcher.LowestCommonAncestor(resolver.Context,
                env.IntType.InstanceOf, env.DoubleType.InstanceOf, out IEntityInstance common);
            Assert.IsTrue(found);
            Assert.AreEqual(env.ObjectType.InstanceOf, common);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter LowestCommonAncestorVoidObject()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var resolver = NameResolver.Create(env);

            bool found = TypeMatcher.LowestCommonAncestor(resolver.Context,
                env.VoidType.InstanceOf, env.ObjectType.InstanceOf, out IEntityInstance common);
            Assert.IsFalse(found);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ParentNamesResolving()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var foo_type = TypeBuilder.Create(NameDefinition.Create("Foo", "V", VarianceMode.None)).Build();
            system_ns.AddNode(foo_type);
            var parent_ref = NameReference.Create("Foo", NameReference.Create("T"));
            var tuple_type = TypeBuilder.Create(NameDefinition.Create("Tuple", "T", VarianceMode.None)).Parents(parent_ref).Build();
            system_ns.AddNode(tuple_type);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, parent_ref.Binding.Matches.Count());
            Assert.AreEqual(foo_type, parent_ref.Binding.Match.Target);
            Assert.AreEqual(tuple_type.NestedTypes.Single(),
                parent_ref.Binding.Match.TemplateArguments.Single().Target());

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorLoopedAncestors()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            system_ns.AddBuilder(TypeBuilder.Create("Foo")
                .Modifier(EntityModifier.Base)
                .Parents(NameReference.Create("Bar")));
            system_ns.AddBuilder(TypeBuilder.Create("Bar")
                .Modifier(EntityModifier.Base)
                .Parents(NameReference.Create("Foo")));

            // if it does not hang, it is OK
            var resolver = NameResolver.Create(env);
            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.CyclicTypeHierarchy, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIncorrectMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("foo"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference()))
                .With(FunctionBuilder.Create(NameDefinition.Create("fin"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.VoidTypeReference(),
                    Block.CreateStatement()))
                .With(FunctionBuilder.CreateDeclaration(NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference()))
                .Modifier(EntityModifier.Interface));

            FunctionDefinition bar_impl = FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(), Block.CreateStatement(new[] {
                        Return.Create(IntLiteral.Create("2"))
                    }));
            FunctionDefinition fin_impl = FunctionBuilder.Create(
                    NameDefinition.Create("fin"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.VoidTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Refines | EntityModifier.UnchainBase);
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(bar_impl)
                .With(fin_impl)
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingFunctionImplementation, type_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingDerivedModifier, bar_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotDeriveSealedMethod, fin_impl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperBasicMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()))
                    .Parameters(FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false)))
                .Modifier(EntityModifier.Interface));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.OptionalUse,
                    // subtype of original result typename -- this is legal
                    NameFactory.PointerTypeReference(NameFactory.IntTypeReference()),
                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout("x"),
                        Return.Create(ExpressionFactory.HeapConstructorCall(NameFactory.IntTypeReference(), IntLiteral.Create("2")))
                    }))
                    .Modifier(EntityModifier.Refines))
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperGenericMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("IX", TemplateParametersBuffer.Create()
                .Add("T")
                .Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference())
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), Variadic.None, null, isNameRequired: false)))
                .Modifier(EntityModifier.Interface));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                .Add("V").Values))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameReference.Create("V"), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout("x"),
                        Return.Create(IntLiteral.Create("2"))
                    }))
                    .Modifier(EntityModifier.Refines))
                .Parents(NameReference.Create("IX", NameReference.Create("V"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperGenericWithCostraintsMethodDerivation()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("IX", TemplateParametersBuffer.Create()
                .Add("T", VarianceMode.None)
                .Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar", TemplateParametersBuffer.Create().Add("U", VarianceMode.None).Values),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference())
                    .Constraints(ConstraintBuilder.Create("U").Inherits(NameReference.Create("T")))
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), Variadic.None, null, isNameRequired: false)))
                .Modifier(EntityModifier.Interface));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                .Add("V", VarianceMode.None)
                .Values))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar", TemplateParametersBuffer.Create().Add("W", VarianceMode.None).Values),
                    new[] { FunctionParameter.Create("x", NameReference.Create("V"), Variadic.None, null, isNameRequired: false) },
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntTypeReference(),
                    Block.CreateStatement(new[] {
                        ExpressionFactory.Readout("x"),
                        Return.Create(IntLiteral.Create("2"))
                    }))
                    .Constraints(ConstraintBuilder.Create("W").Inherits(NameReference.Create("V")))
                    .Modifier(EntityModifier.Refines))
                .Parents(NameReference.Create("IX", NameReference.Create("V"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}