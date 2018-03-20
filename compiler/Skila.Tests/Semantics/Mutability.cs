using Microsoft.VisualStudio.TestTools.UnitTesting;
using NaiveLanguageTools.Common;
using Skila.Language;
using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Expressions.Literals;
using Skila.Language.Extensions;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System.Linq;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Mutability
    {
        [TestMethod]
        public IErrorReporter ErrorTransitiveMutabilityTypeInheritance()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Alien")
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Container", "CCC", VarianceMode.None))
                .SetModifier(EntityModifier.Base));

            // we should get error here because the parent should evaluate to mutable type, and current one is non-mutable
            NameReference parent_name = NameReference.Create("Container", NameReference.Create("Alien"));
            root_ns.AddBuilder(TypeBuilder.Create("Done")
                .Parents(parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_name));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTransitiveMutabilityTypePassing()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Alien")
                .SetModifier(EntityModifier.Mutable));

            // Option is a type which mutability is not decided until concrete type is given
            // here we pass mutable type so it should evaluate into mutable one 
            // and next it should create an error when passing to const type
            VariableDeclaration decl_bad = VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(MutabilityOverride.ForceConst)),
                        ExpressionFactory.OptionEmpty("Alien", Memory.Heap));

            root_ns.AddBuilder(TypeBuilder.Create("Cool"));

            VariableDeclaration decl_ok = VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(MutabilityOverride.ForceConst)),
                        ExpressionFactory.OptionEmpty("Cool", Memory.Heap));

            root_ns.AddBuilder(FunctionBuilder.Create(NameFactory.MainFunctionName, NameFactory.Nat8TypeReference(),
                Block.CreateStatement(
                    decl_bad,
                    ExpressionFactory.Readout("x"),
                    decl_ok,
                    ExpressionFactory.Readout("y"),
                    Return.Create(Nat8Literal.Create("0"))
                    )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, decl_bad.InitValue));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCastingWithMutabilityChange()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Alien")
                .SetModifier(EntityModifier.Mutable));

            // since Alien is mutable it looks like we try to shake off neutral flag
            IExpression bad_cast = ExpressionFactory.DownCast(NameReference.Create("x"),
                            NameFactory.PointerTypeReference(NameReference.Create("Alien")));
            ReinterpretType bad_reinterpret = bad_cast.DescendantNodes().WhereType<ReinterpretType>().Single();

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("x",

                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(MutabilityOverride.Neutral)),
                        Undef.Create()),
                    VariableDeclaration.CreateStatement("c", null, bad_cast),
                    ExpressionFactory.Readout("c"),

                    VariableDeclaration.CreateStatement("ok", null,
                        ExpressionFactory.DownCast(NameReference.Create("x"),
                            NameFactory.PointerTypeReference(NameReference.Create(MutabilityOverride.Neutral, "Alien")))),
                    ExpressionFactory.Readout("ok")
            )));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, bad_reinterpret));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAbusingForcedConst()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Stone"));

            root_ns.AddBuilder(TypeBuilder.Create("Mutator", "M")
                .With(FunctionBuilder.Create("violate", NameFactory.UnitTypeReference(), Block.CreateStatement())
                    .SetModifier(EntityModifier.Mutable))
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Mangler")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("m",
                    NameFactory.PointerTypeReference(NameReference.Create("Mutator", NameReference.Create("Stone"))),
                    Undef.Create(),
                    EntityModifier.Public | EntityModifier.Reassignable))
                    );

            FunctionCall mut_call = FunctionCall.Create(NameReference.CreateThised("f", "m", "violate"));
            IExpression assignment = Assignment.CreateStatement(NameReference.CreateThised("f", "m"), Undef.Create());
            root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                .With(VariableDeclaration.CreateStatement("f",
                    NameFactory.PointerTypeReference(NameReference.Create(MutabilityOverride.ForceConst, "Mangler")),
                    Undef.Create(),
                    EntityModifier.Public))
                 .With(FunctionBuilder.Create("testing", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        mut_call,
                        assignment
                    ))));



            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, mut_call));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, assignment));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ForcingConstIndirectly()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Stone"));

            root_ns.AddBuilder(TypeBuilder.Create("Mutator", "M")
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Mangler")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("f",
                    NameFactory.PointerTypeReference(NameReference.Create("Mutator", NameReference.Create("Stone"))),
                    Undef.Create(),
                    EntityModifier.Public))
                    );

            root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                .With(VariableDeclaration.CreateStatement("f",
                    NameFactory.PointerTypeReference(NameReference.Create(MutabilityOverride.ForceConst, "Mangler")),
                    Undef.Create(),
                    EntityModifier.Public))
                    );

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ForcingConstDirectly()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Stone"));

            root_ns.AddBuilder(TypeBuilder.Create("Mutator", "M")
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                .With(VariableDeclaration.CreateStatement("f",
                    NameFactory.PointerTypeReference(NameReference.Create(MutabilityOverride.ForceConst, "Mutator", NameReference.Create("Stone"))),
                    Undef.Create(),
                    EntityModifier.Public))
                    );

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorForcingConst()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            NameReference rhs_value = NameReference.Create("a");

            root_ns.AddBuilder(FunctionBuilder.Create("innocent",
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        // this is ok, we are assigning literal here
                        VariableDeclaration.CreateStatement("x",
                            NameFactory.StringPointerTypeReference(MutabilityOverride.ForceConst), StringLiteral.Create("hi"),
                            EntityModifier.Reassignable),
                        VariableDeclaration.CreateStatement("a", null, StringLiteral.Create("no")),
                        // this is not ok, we are assigning mutable to const
                        Assignment.CreateStatement(NameReference.Create("x"), rhs_value),
                        ExpressionFactory.Readout("x")
                        )));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, rhs_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableMethodCallingMutable()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            FunctionCall call = FunctionCall.Create(NameReference.CreateThised("mutator"));
            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .SetModifier(EntityModifier.Mutable)
                .With(FunctionBuilder.Create("innocent", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(call)))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .SetModifier(EntityModifier.Mutable)));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CallingMutableFromImmutableMethod, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingMutablesOnNeutral()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .SetModifier(EntityModifier.Mutable)
                .With(PropertyBuilder.CreateAutoFull("numi", NameFactory.Int64TypeReference(), null))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement())
                    .SetModifier(EntityModifier.Mutable)));

            FunctionCall call = FunctionCall.Create(NameReference.Create("x", "mutator"));
            IExpression assignment = Assignment.CreateStatement(NameReference.Create("x", "numi"), Int64Literal.Create("5"));
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                NameFactory.UnitTypeReference(),
                Block.CreateStatement(
                    // we can assign both mutable and immutable to neutral
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameReference.Create(MutabilityOverride.Neutral, "Elka")),
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
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            IExpression assignment = Assignment.CreateStatement(NameReference.CreateThised("f"), Int64Literal.Create("5"));
            root_ns.AddBuilder(TypeBuilder.Create("Elka")
                .SetModifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("f", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable))
                .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        assignment
                        ))));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringCurrentInImmutableMethod, assignment));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableMethodInImmutableType()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            Property property = PropertyBuilder.Create("bar", NameFactory.Int64TypeReference())
                    .WithSetter(body: null);
            FunctionDefinition function = FunctionBuilder.CreateDeclaration("getMe", NameFactory.UnitTypeReference())
                    .SetModifier(EntityModifier.Mutable);
            root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                .SetModifier(EntityModifier.Abstract)
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
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;


            Return ret = Return.Create(NameReference.Create("coll"));
            root_ns.AddBuilder(FunctionBuilder.Create(NameDefinition.Create("laundering", "T", VarianceMode.None),
               new[] { FunctionParameter.Create("coll",
                    NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", mutability:  MutabilityOverride.ForceMutable))) },
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
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    // we can assign both mutable and immutable to neutral
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( MutabilityOverride.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    ExpressionFactory.Readout("x"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( MutabilityOverride.Neutral)),
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
                .SetModifier(EntityModifier.Mutable));

            root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

            NameReference init_value_x = NameReference.Create("x");
            NameReference init_value_y = NameReference.Create("y");
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( MutabilityOverride.Neutral)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    VariableDeclaration.CreateStatement("xx",
                        NameFactory.PointerTypeReference(NameReference.Create("Mutant")),init_value_x),
                    ExpressionFactory.Readout("xx"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( MutabilityOverride.Neutral)),
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
                .SetModifier(EntityModifier.Mutable));

            IExpression mutable_init = ExpressionFactory.HeapConstructor(NameReference.Create("Bar"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference()),
                        mutable_init),
                    ExpressionFactory.Readout("x"),
                    // this is OK, we mark target as mutable type and we pass indeed mutable one
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(overrideMutability: MutabilityOverride.ForceMutable)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Bar"))),
                    ExpressionFactory.Readout("y"),
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, mutable_init));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableTypeDefinition()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .SetModifier(EntityModifier.Mutable));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.Int64TypeReference(),
                null, EntityModifier.Reassignable | EntityModifier.Public);
            VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("Bar"),
                Undef.Create(), modifier: EntityModifier.Public);
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
               .With(decl1)
               .With(decl2));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReassignableFieldInImmutableType, decl1));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFieldInImmutableType, decl2));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TransitiveMutabilityTypeDefinition()
        {
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            VariableDeclaration decl = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                Undef.Create(), modifier: EntityModifier.Public);
            // we can declare type Point as non-mutable, because it will be mutable or not depending on T
            TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point", "T")
               .With(decl));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorViolatingConstConstraint()
        {
            var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .SetModifier(EntityModifier.Mutable));
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
            var env = Language.Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Bar")
                .SetModifier(EntityModifier.Mutable));

            VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.Int64TypeReference(),
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
            var env = Language.Environment.Create(new Options() { });
            var root_ns = env.Root;

            root_ns.AddBuilder(TypeBuilder.Create("Parent")
                .SetModifier(EntityModifier.Base | EntityModifier.Mutable));

            NameReference parent_name = NameReference.Create("Parent");
            root_ns.AddBuilder(TypeBuilder.Create("Child")
                .Parents(parent_name));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_name));

            return resolver;
        }
    }
}
