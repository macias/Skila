﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Flow;
using Skila.Language.Semantics;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Properties
    {
        [TestMethod]
        public IErrorReporter ErrorAssigningRValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.IntTypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.IntTypeReference(), IntLiteral.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.IntTypeReference()) },
                    new[] { Property.CreateAutoSetter(NameFactory.IntTypeReference()) }
                )));

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getter"),
                null,
                ExpressionReadMode.ReadRequired,
                NameReference.Create("Point"),
                Block.CreateStatement(new[] { Return.Create(Undef.Create()) })));

            NameReference field_ref = NameReference.Create(FunctionCall.Create(NameReference.Create("getter")), "x");
            root_ns.AddNode(Block.CreateStatement(new IExpression[] {
                // error: assigning to r-value
                Assignment.CreateStatement(field_ref,IntLiteral.Create("3")),
            }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, field_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMultipleAccessors()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            FunctionDefinition mul_getter = Property.CreateAutoGetter(NameFactory.IntTypeReference());
            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.IntTypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.IntTypeReference(), IntLiteral.Create("1"), EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.IntTypeReference()), mul_getter },
                    new[] { Property.CreateAutoSetter(NameFactory.IntTypeReference()) }
                )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PropertyMultipleAccessors, mul_getter));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAlteringReadOnlyProperty()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .With(Property.Create("x", NameFactory.IntTypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.IntTypeReference(), IntLiteral.Create("1")) },
                    new[] { Property.CreateAutoGetter(NameFactory.IntTypeReference()) },
                    setters: null
                )));

            Assignment assignment = Assignment.CreateStatement(NameReference.Create("p", "x"), IntLiteral.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("notimportant"),
                ExpressionReadMode.OptionalUse,
                NameFactory.VoidTypeReference(),
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
