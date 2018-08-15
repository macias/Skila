using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Expressions.Literals;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Properties : ITest
    {
        [TestMethod]
        public IErrorReporter ErrorSettingCustomGetter()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                // we can assign property using getter (only in constructor) but getter has to be auto-generated, 
                // here it is custom code, thus it is illegal
                IExpression assign = Assignment.CreateStatement(NameReference.CreateThised("x"), IntLiteral.Create("3"));

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.Create(env.Options, "x", ()=>NameFactory.IntNameReference())
                        .WithGetter(Block.CreateStatement(Return.Create(IntLiteral.Create("5")))))
                    .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                        assign
                        ))));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotAssignCustomProperty, assign));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorGetterOverridesNothing()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options().SetMutability(mutability));
                var root_ns = env.Root;

                Property property = PropertyBuilder.Create(env.Options, "getMe", ()=>NameFactory.Int64NameReference())
                        .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                            .Modifier(EntityModifier.Override));

                var type = root_ns.AddBuilder(TypeBuilder.Create("Last")
                    .SetModifier(EntityModifier.Base)
                    .With(property));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToOverride, property.Getter));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningRValue()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(Property.Create(env.Options, "x", NameFactory.Int64NameReference(),
                        new[] { Property.CreateAutoField(NameFactory.Int64NameReference(), Int64Literal.Create("1"), 
                            env.Options.ReassignableModifier()) },
                        new[] { Property.CreateAutoGetter(NameFactory.Int64NameReference()) },
                        new[] { Property.CreateAutoSetter(NameFactory.Int64NameReference()) }
                    )));

                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "getter",
                    null,
                    NameReference.Create("Point"),
                    Block.CreateStatement(new[] { Return.Create(Undef.Create()) })));

                NameReference field_ref = NameReference.Create(FunctionCall.Create(NameReference.Create("getter")), "x");
                root_ns.AddNode(Block.CreateStatement(new IExpression[] {
                // error: assigning to r-value
                Assignment.CreateStatement(field_ref,Int64Literal.Create("3")),
            }));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, field_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringGetter()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.Create(env.Options, "x", ()=>NameFactory.Int64NameReference())
                        .WithAutoField(Int64Literal.Create("1"), env.Options.ReassignableModifier())
                        .WithAutoGetter()
                        .WithAutoSetter()));

                NameReference getter_call = NameReference.Create("p", "x");
                root_ns.AddBuilder(FunctionBuilder.Create(
                    "getter",
                    null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null, ExpressionFactory.StackConstructor("Point")),
                    getter_call
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ExpressionValueNotUsed, getter_call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMultipleAccessors()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionDefinition mul_getter = Property.CreateAutoGetter(NameFactory.Int64NameReference());
                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .SetModifier(EntityModifier.Mutable)
                    .With(Property.Create(env.Options, "x", NameFactory.Int64NameReference(),
                        new[] { Property.CreateAutoField(NameFactory.Int64NameReference(), Int64Literal.Create("1"), 
                            env.Options.ReassignableModifier()) },
                        new[] { Property.CreateAutoGetter(NameFactory.Int64NameReference()), mul_getter },
                        new[] { Property.CreateAutoSetter(NameFactory.Int64NameReference()) }
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PropertyMultipleAccessors, mul_getter));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAlteringReadOnlyProperty()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                    .With(Property.Create(env.Options, "x", NameFactory.Int64NameReference(),
                        new[] { Property.CreateAutoField(NameFactory.Int64NameReference(), Int64Literal.Create("1")) },
                        new[] { Property.CreateAutoGetter(NameFactory.Int64NameReference()) },
                        setters: null
                    )));

                IExpression assignment = Assignment.CreateStatement(NameReference.Create("p", "x"), Int64Literal.Create("5"));
                root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p", NameReference.Create("Point"), Undef.Create()),
                    assignment,
                    })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, assignment));
            }

            return resolver;
        }
    }
}
