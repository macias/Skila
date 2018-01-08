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
    public class Mutability
    {
        [TestMethod]
        public IErrorReporter ErrorImmutableMethodCallingMutable()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            FunctionCall call = FunctionCall.Create(NameReference.CreateThised("mutator"));
            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .Modifier(EntityModifier.Mutable)
                .With(FunctionBuilder.Create("innocent", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(call)))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Mutable)));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CallingMutableFromImmutableMethod, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingMutablesOnNeutral()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .Modifier(EntityModifier.Mutable)
                .With(PropertyBuilder.CreateAutoFull("numi",NameFactory.IntTypeReference(),null))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .Modifier(EntityModifier.Mutable)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("x", "mutator"));
            IExpression assignment = Assignment.CreateStatement(NameReference.Create("x", "numi"), IntLiteral.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    // we can assign both mutable and immutable to neutral
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameReference.Create(MutabilityFlag.Neutral,"Elka")),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Elka"))),
                    call,
                    assignment
            )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, call));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, assignment));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableMethodAlteringData()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            IExpression assignment = Assignment.CreateStatement(NameReference.CreateThised("f"), IntLiteral.Create("5"));
            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("f", NameFactory.IntTypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        assignment
                        ))));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringInstanceInImmutableMethod, assignment));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableMethodInImmutableType()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            Property property = PropertyBuilder.Create("bar", NameFactory.IntTypeReference())
                    .WithSetter(body: null);
            FunctionDefinition function = FunctionBuilder.CreateDeclaration("getMe", NameFactory.UnitTypeReference())
                    .Modifier(EntityModifier.Mutable);
            root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                .Modifier(EntityModifier.Abstract)
                .With(property)
                .With(function));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PropertySetterInImmutableType, property));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFunctionInImmutableType, function));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutabilityLaunderingOnReturn()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;


            Return ret = Return.Create(NameReference.Create("coll"));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("laundering", "T", VarianceMode.None),
               new[] { FunctionParameter.Create("coll",
                    NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", overrideMutability:  MutabilityFlag.ForceMutable))) },
               ExpressionReadMode.ReadRequired,
               NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T")),
               Block.CreateStatement(new IExpression[] {
                       ret
               })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, ret.Expr));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AssigningToNeutral()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Mutant")
                .Modifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    // we can assign both mutable and immutable to neutral
                    VariableDeclaration.CreateStatement("x", 
                        NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference( MutabilityFlag.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    ExpressionFactory.Readout("x"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference( MutabilityFlag.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Untouchable"))),
                    ExpressionFactory.Readout("y"),
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningNeutrals()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Mutant")
                .Modifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

            NameReference init_value_x = NameReference.Create("x");
            NameReference init_value_y = NameReference.Create("y");
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference( MutabilityFlag.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    VariableDeclaration.CreateStatement("xx",
                        NameFactory.PointerTypeReference(NameReference.Create("Mutant")),init_value_x),
                    ExpressionFactory.Readout("xx"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference( MutabilityFlag.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Untouchable"))),
                    VariableDeclaration.CreateStatement("yy",
                        NameFactory.PointerTypeReference(NameReference.Create("Untouchable")),init_value_y),
                    ExpressionFactory.Readout("yy"),
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value_x));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value_y));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningMutableToImmutable()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .Modifier(EntityModifier.Mutable));

            IExpression mutable_init = ExpressionFactory.HeapConstructor(NameReference.Create("Bar"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference()),
                        mutable_init),
                    ExpressionFactory.Readout("x"),
                    // this is OK, we mark target as mutable type and we pass indeed mutable one
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.ObjectTypeReference(overrideMutability: MutabilityFlag.ForceMutable)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Bar"))),
                    ExpressionFactory.Readout("y"),
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, mutable_init));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableTypes()
        {
            var env = Language.Environment.Create();
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .Modifier(EntityModifier.Mutable));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.IntTypeReference(),
                null, EntityModifier.Reassignable | EntityModifier.Public);
            VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                Undef.Create(), modifier: EntityModifier.Public);
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point", "T")
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
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .Modifier(EntityModifier.Mutable));
            root_ns.AddBuilder(TypeBuilder.Create("Foo"));

            // we build type Point<T> with enforced "const" on T -- meaning we can pass only trully immutable types
            // as T
            VariableDeclaration field = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                Undef.Create(), modifier: EntityModifier.Public);
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Point",
                    TemplateParametersBuffer.Create().Add("T").Values))
               .Constraints(ConstraintBuilder.Create("T")
                    .Modifier(EntityModifier.Const))
               .With(field));

            // Bar is mutable type, so we cannot construct Point<Bar> since Point requires immutable type
            NameReference wrong_type = NameReference.Create("Point", NameReference.Create("Bar"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameReference.Create("Point",NameReference.Create("Foo")), Undef.Create()),
                    VariableDeclaration.CreateStatement("y", wrong_type, Undef.Create()),
                    ExpressionFactory.Readout("x"),
                    ExpressionFactory.Readout("y"),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedConstConstraint, wrong_type));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableGlobalVariables()
        {
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .Modifier(EntityModifier.Mutable));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.IntTypeReference(),
                null, EntityModifier.Reassignable | EntityModifier.Public);
            VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("Bar"),
                null, modifier: EntityModifier.Public);

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

            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                .Modifier(EntityModifier.Base | EntityModifier.Mutable));

            NameReference parent_name = NameReference.Create("Parent");
            root_ns.AddBuilder(TypeBuilder.Create("Child")
                .Parents(parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ImmutableInheritsMutable, parent_name));

            return resolver;
        }
    }
}
