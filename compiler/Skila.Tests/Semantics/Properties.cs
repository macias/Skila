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
    public class Properties
    {
        [TestMethod]
        public IErrorReporter ErrorSettingCustomGetter()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            // we can assign property using getter (only in constructor) but getter has to be auto-generated, 
            // here it is custom code, thus it is illegal
            IExpression assign = Assignment.CreateStatement(NameReference.CreateThised("x"), IntLiteral.Create("3"));

            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable)
                .With(PropertyBuilder.Create("x", NameFactory.IntTypeReference())
                    .WithGetter(Block.CreateStatement(Return.Create(IntLiteral.Create("5")))))
                .With(FunctionBuilder.CreateInitConstructor(Block.CreateStatement(
                    assign
                    ))));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotAssignCustomProperty, assign));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorGetterOverridesNothing()
        {
            var env = Language.Environment.Create(new Options());
            var root_ns = env.Root;

            Property property = PropertyBuilder.Create("getMe", NameFactory.Int64TypeReference())
                    .With(PropertyMemberBuilder.CreateGetter(Block.CreateStatement(Return.Create(Int64Literal.Create("2"))))
                        .Modifier(EntityModifier.Override));

            var type = root_ns.AddBuilder(TypeBuilder.Create("Last")
                .SetModifier(EntityModifier.Base)
                .With(property));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NothingToOverride, property.Getter));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningRValue()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.Int64TypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.Int64TypeReference(), Int64Literal.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.Int64TypeReference()) },
                    new[] { Property.CreateAutoSetter(NameFactory.Int64TypeReference()) }
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

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringGetter()
        {
            var env = Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable)
                .With(PropertyBuilder.Create("x", NameFactory.Int64TypeReference())
                    .WithAutoField(Int64Literal.Create("1"), EntityModifier.Reassignable)
                    .WithAutoGetter()
                    .WithAutoSetter()));

            NameReference getter_call = NameReference.Create("p", "x");
            root_ns.AddBuilder(FunctionBuilder.Create(
                "getter",
                null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p",null,ExpressionFactory.StackConstructor("Point")),
                    getter_call
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ExpressionValueNotUsed, getter_call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMultipleAccessors()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            FunctionDefinition mul_getter = Property.CreateAutoGetter(NameFactory.Int64TypeReference());
            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .SetModifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.Int64TypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.Int64TypeReference(), Int64Literal.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.Int64TypeReference()), mul_getter },
                    new[] { Property.CreateAutoSetter(NameFactory.Int64TypeReference()) }
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PropertyMultipleAccessors, mul_getter));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAlteringReadOnlyProperty()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .With(Property.Create("x", NameFactory.Int64TypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.Int64TypeReference(), Int64Literal.Create("1")) },
                    new[] { Property.CreateAutoGetter(NameFactory.Int64TypeReference()) },
                    setters: null
                )));

            IExpression assignment = Assignment.CreateStatement(NameReference.Create("p", "x"), Int64Literal.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("p", NameReference.Create("Point"), Undef.Create()),
                    assignment,
                })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, assignment));

            return resolver;
        }
    }
}
