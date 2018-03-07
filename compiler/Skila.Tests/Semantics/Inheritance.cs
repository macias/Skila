using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Extensions;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Inheritance
    {
        [TestMethod]
        public IErrorReporter DeepInheritanceWithPrivateInterfaceFunction()
        {
            // testing whether we can call `super` when inheriting private function -- we should

            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ISecret")
                .With(FunctionBuilder.CreateDeclaration("noTell", NameFactory.UnitTypeReference())
                    .Modifier(EntityModifier.Private))
            );

            root_ns.AddBuilder(TypeBuilder.Create("CardboardBox")
                .Parents("ISecret")
                .SetModifier(EntityModifier.Base)
                // refining private is OK
                .With(FunctionBuilder.Create("noTell", NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Override | EntityModifier.Private))
                );

            root_ns.AddBuilder(TypeBuilder.Create("Submarine")
                .Parents("CardboardBox")
                // refining private is OK
                .With(FunctionBuilder.Create("noTell", NameFactory.UnitTypeReference(),
                    // we should be able to call super
                    Block.CreateStatement(FunctionCall.Create(NameReference.Create(NameFactory.SuperFunctionName))))
                    .Modifier(EntityModifier.Override | EntityModifier.Private))
                );

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorHeapModifierOnOverride()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Grandparent")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create("f",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .Modifier(EntityModifier.Base)
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead))));

            FunctionDefinition func = FunctionBuilder.Create("f",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .Modifier(EntityModifier.Override | EntityModifier.UnchainBase | EntityModifier.HeapOnly)
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead));
            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                    .SetModifier(EntityModifier.Base)
                    .Parents("Grandparent")
                    .With(func));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.HeapRequirementChangedOnOverride, func));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter MethodNoDominance()
        {
            // https://en.wikipedia.org/wiki/Dominance_(C%2B%2B)#Example_without_diamond_inheritance

            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Grandparent")
                    .SetModifier(EntityModifier.Base)
                    .With(FunctionBuilder.Create("f",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead)))
                    .With(FunctionBuilder.Create("f",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .Parameters(FunctionParameter.Create("a", NameFactory.RealTypeReference(), ExpressionReadMode.CannotBeRead),
                            FunctionParameter.Create("b", NameFactory.RealTypeReference(), ExpressionReadMode.CannotBeRead))));

            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                    .SetModifier(EntityModifier.Base)
                    .Parents("Grandparent")
                    .With(FunctionBuilder.Create("f",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .Parameters(FunctionParameter.Create("x", NameFactory.Int64TypeReference(), ExpressionReadMode.CannotBeRead))));

            root_ns.AddBuilder(TypeBuilder.Create("Child")
                    .Parents("Parent")
                    .With(FunctionBuilder.Create("g",
                     NameFactory.UnitTypeReference(),
                        Block.CreateStatement(
                            // unlike C++ this method is seen as regular overload
                            FunctionCall.Create(NameReference.CreateThised("f"),
                                RealLiteral.Create("2.14"), RealLiteral.Create("3.17"))))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMissingPinnedDefinition()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Start")
                .SetModifier(EntityModifier.Base)
                .With(FunctionBuilder.Create("getSome",
                 NameFactory.Int64TypeReference(),
                    Block.CreateStatement(Return.Create(Int64Literal.Create("3"))))
                    .Modifier(EntityModifier.Pinned)));

            root_ns.AddBuilder(TypeBuilder.Create("Middle")
                .SetModifier(EntityModifier.Base)
                .Parents("Start")
                .With(FunctionBuilder.Create("getSome",
                 NameFactory.Int64TypeReference(),
                    Block.CreateStatement(Return.Create(Int64Literal.Create("3"))))
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase)));

            TypeDefinition end_type = root_ns.AddBuilder(TypeBuilder.Create("End")
                .Parents("Middle"));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VirtualFunctionMissingImplementation, end_type));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInvalidDirectionPassingEnums()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateEnum("Weekend")
                .With(EnumCaseBuilder.Create("Sat", "Sun"))
                .SetModifier(EntityModifier.Base));

            root_ns.AddBuilder(TypeBuilder.CreateEnum("First")
                .With(EnumCaseBuilder.Create("Mon"))
                .Parents("Weekend"));

            IExpression init_value = ExpressionFactory.HeapConstructor("First", NameReference.Create("First", "Mon"));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("some"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("a",NameFactory.PointerTypeReference( NameReference.Create("Weekend")),
                        init_value),
                    ExpressionFactory.Readout("a")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorEnumCrossInheritance()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                .SetModifier(EntityModifier.Base));

            TypeDefinition from_reg = root_ns.AddBuilder(TypeBuilder.CreateEnum("Sizing")
                .Parents("Whatever")
                .SetModifier(EntityModifier.Base)
                .With(EnumCaseBuilder.Create("small", "big")));

            TypeDefinition from_enum = root_ns.AddBuilder(TypeBuilder.Create("Another")
                .Parents("Sizing")
                .SetModifier(EntityModifier.Base));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EnumCrossInheritance, from_enum));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EnumCrossInheritance, from_reg));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNonVirtualInterfacePattern()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface("ITransmogrifier")
                .With(FunctionBuilder.CreateDeclaration("transmogrify", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference()

                )
                    .Modifier(EntityModifier.Private))
                .With(FunctionBuilder.CreateDeclaration("untransmogrify", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference()

                )
                    .Modifier(EntityModifier.Private))
            );

            NameReference private_reference = NameReference.Create(NameFactory.ThisVariableName, "transmogrify");
            // refining private is OK, but we cannot change the access to it
            FunctionDefinition public_func = FunctionBuilder.Create("untransmogrify", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement())
                .Modifier(EntityModifier.Override);

            root_ns.AddBuilder(TypeBuilder.Create("CardboardBox")
                .Parents("ITransmogrifier")
                // refining private is OK
                .With(FunctionBuilder.Create("transmogrify", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement())
                    .Modifier(EntityModifier.Override | EntityModifier.Private))
                .With(public_func)
                // but using it -- not
                .With(FunctionBuilder.Create("trying", ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),

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
        public IErrorReporter ErrorNothingToOverride()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            FunctionDefinition function = FunctionBuilder.Create("getSome",
                ExpressionReadMode.ReadRequired, NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("3"))
                    }))
                    .Modifier(EntityModifier.Override);
            root_ns.AddBuilder(TypeBuilder.Create("GetPos")
                .With(function));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToOverride, function));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorInheritingHeapOnlyType()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point"))
                .SetModifier(EntityModifier.HeapOnly | EntityModifier.Base));

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
            var env = Language.Environment.Create(new Options() { });
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
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point1")).SetModifier(EntityModifier.Base));
            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point2")).SetModifier(EntityModifier.Base));

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
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            var abc_type = root_ns.AddBuilder(TypeBuilder.Create("ABC"));
            var foo_type = root_ns.AddBuilder(TypeBuilder.Create("Foo").Parents(NameReference.Create("ABC")));
            var bar_type = root_ns.AddBuilder(TypeBuilder.Create("Bar").Parents(NameReference.Create("ABC")));
            var deriv_type = root_ns.AddBuilder(TypeBuilder.Create("Deriv").Parents(NameReference.Create("Foo")));

            var deriv_ref = root_ns.AddNode(NameReference.Create("Deriv"));
            var bar_ref = root_ns.AddNode(NameReference.Create("Bar"));
            var abc_ref = root_ns.AddNode(NameReference.Create("ABC"));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(bar_type, bar_ref.Binding.Match.Instance.Target);
            Assert.AreEqual(deriv_type, deriv_ref.Binding.Match.Instance.Target);
            Assert.AreEqual(abc_type, abc_ref.Binding.Match.Instance.Target);

            bool found = TypeMatcher.LowestCommonAncestor(resolver.Context,
                bar_ref.Binding.Match.Instance, deriv_ref.Binding.Match.Instance, out IEntityInstance common);
            Assert.IsTrue(found);
            Assert.AreEqual(abc_ref.Binding.Match.Instance, common);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter LowestCommonAncestorBoolInt()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            var resolver = NameResolver.Create(env);

            bool found = TypeMatcher.LowestCommonAncestor(resolver.Context,
                env.Int64Type.InstanceOf, env.BoolType.InstanceOf, out IEntityInstance common);
            Assert.IsTrue(found);
            Assert.AreEqual(env.IEquatableType.InstanceOf, common);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ParentNamesResolving()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var foo_type = TypeBuilder.Create(NameDefinition.Create("Foo", "V", VarianceMode.None)).Build();
            system_ns.AddNode(foo_type);
            var parent_ref = NameReference.Create("Foo", NameReference.Create("T"));
            var tuple_type = TypeBuilder.Create(NameDefinition.Create("Tuple", "T", VarianceMode.None)).Parents(parent_ref).Build();
            system_ns.AddNode(tuple_type);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, parent_ref.Binding.Matches.Count());
            Assert.AreEqual(foo_type, parent_ref.Binding.Match.Instance.Target);
            Assert.AreEqual(tuple_type.NestedTypes().Single(),
                parent_ref.Binding.Match.Instance.TemplateArguments.Single().Target());

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorLoopedAncestors()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            system_ns.AddBuilder(TypeBuilder.Create("Foo")
                .SetModifier(EntityModifier.Base)
                .Parents(NameReference.Create("Bar")));
            system_ns.AddBuilder(TypeBuilder.Create("Bar")
                .SetModifier(EntityModifier.Base)
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
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("foo"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference()))
                .With(FunctionBuilder.Create(NameDefinition.Create("fin"),
                    ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement()))
                .With(FunctionBuilder.CreateDeclaration(NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference()))
                .SetModifier(EntityModifier.Interface));

            FunctionDefinition bar_impl = FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(), Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }));
            FunctionDefinition fin_impl = FunctionBuilder.Create(
                    NameDefinition.Create("fin"),
                    ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                    Block.CreateStatement())
                    .Modifier(EntityModifier.Override | EntityModifier.UnchainBase);
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(bar_impl)
                .With(fin_impl)
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(3, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VirtualFunctionMissingImplementation, type_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MissingOverrideModifier, bar_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotOverrideSealedMethod, fin_impl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMissingFunctionImplementation()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Inter")
                .With(FunctionBuilder.CreateDeclaration(NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference()))
                .SetModifier(EntityModifier.Interface));

            root_ns.AddBuilder(TypeBuilder.Create("MiddleImpl")
                // ok to ignore the functions inside abstract type
                .SetModifier(EntityModifier.Abstract)
                .Parents(NameReference.Create("Inter")));

            // there is still function to implement
            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("Impl")
                .Parents(NameReference.Create("MiddleImpl")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VirtualFunctionMissingImplementation, type_impl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperBasicMethodOverride()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("IX")
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference()))
                    .Parameters(FunctionParameter.Create("x", NameFactory.BoolTypeReference(), Variadic.None, null, isNameRequired: false)))
                .SetModifier(EntityModifier.Interface));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create("X")
                .With(FunctionBuilder.Create(NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameFactory.BoolTypeReference(), usageMode: ExpressionReadMode.CannotBeRead) },
                    ExpressionReadMode.OptionalUse,
                    // subtype of original result typename -- this is legal
                    NameFactory.PointerTypeReference(NameFactory.Int64TypeReference()),
                    Block.CreateStatement(new[] {
                        Return.Create(ExpressionFactory.HeapConstructor(NameFactory.Int64TypeReference(), Int64Literal.Create("2")))
                    }))
                    .Modifier(EntityModifier.Override))
                .Parents(NameReference.Create("IX")));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperGenericMethodOverride()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("IX", TemplateParametersBuffer.Create()
                .Add("T")
                .Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference())
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("T"), Variadic.None, null, isNameRequired: false)))
                .SetModifier(EntityModifier.Interface));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("X", TemplateParametersBuffer.Create()
                .Add("V").Values))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    new[] { FunctionParameter.Create("x", NameReference.Create("V"), usageMode: ExpressionReadMode.CannotBeRead) },
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))
                    .Modifier(EntityModifier.Override))
                .Parents(NameReference.Create("IX", NameReference.Create("V"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperGenericMethodOverrideWithGenericOutput()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("IMyInterface",
                    TemplateParametersBuffer.Create().Add("TI").Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceTypeReference(NameReference.Create("TI")))));

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("MyImpl",
                    TemplateParametersBuffer.Create().Add("MV").Values))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceTypeReference(NameReference.Create("MV")),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Override))
                .Parents(NameReference.Create("IMyInterface", NameReference.Create("MV"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMixedMemoryClassOverrideWithGenericOutput()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            // here we define to return reference and value of generic type
            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("IMyInterface",
                    TemplateParametersBuffer.Create().Add("TI").Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameReference.Create("TI")))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("foo"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceTypeReference(NameReference.Create("TI")))));

            // and here we override the above functions but with changed output -- in case of reference we try to set result type as value
            // and in case of function with value output we try to set result type as reference
            // in short wy try to make such overrides (for result types)
            // V -> &V
            // &V -> V
            FunctionDefinition func1_impl = FunctionBuilder.Create(
                    NameDefinition.Create("bar"),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceTypeReference(NameReference.Create("MV")),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Override);
            FunctionDefinition func2_impl = FunctionBuilder.Create(
                    NameDefinition.Create("foo"),
                    ExpressionReadMode.OptionalUse,
                    NameReference.Create("MV"),
                    Block.CreateStatement(new[] {
                        Return.Create(Undef.Create())
                    }))
                    .Modifier(EntityModifier.Override);

            TypeDefinition type_impl = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("MyImpl",
                    TemplateParametersBuffer.Create().Add("MV").Values))
                .With(func1_impl)
                .With(func2_impl)
                .Parents(NameReference.Create("IMyInterface", NameReference.Create("MV"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VirtualFunctionMissingImplementation, type_impl, 2));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToOverride, func1_impl));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToOverride, func2_impl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperGenericWithCostraintsMethodOverride()
        {
            var env = Environment.Create(new Options() { DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.CreateInterface(NameDefinition.Create("IBar",
                TemplateParametersBuffer.Create("TA").Values))
                .With(FunctionBuilder.CreateDeclaration(
                    NameDefinition.Create("barend", TemplateParametersBuffer.Create("FA").Values),
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference())
                    .Constraints(ConstraintBuilder.Create("FA").Inherits(NameReference.Create("TA")))
                    .Parameters(FunctionParameter.Create("x", NameReference.Create("TA")))));

            TypeDefinition type_impl = root_ns.AddBuilder(
                TypeBuilder.Create(NameDefinition.Create("Impl", TemplateParametersBuffer.Create("TB").Values))
                .With(FunctionBuilder.Create(
                    NameDefinition.Create("barend", TemplateParametersBuffer.Create("FB").Values),
                    new[] { FunctionParameter.Create("x", NameReference.Create("TB"),
                        usageMode: ExpressionReadMode.CannotBeRead) },
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64TypeReference(),
                    Block.CreateStatement(new[] {
                        Return.Create(Int64Literal.Create("2"))
                    }))
                    .Constraints(ConstraintBuilder.Create("FB").Inherits(NameReference.Create("TB")))
                    .Modifier(EntityModifier.Override))
                .Parents(NameReference.Create("IBar", NameReference.Create("TB"))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
    }
}