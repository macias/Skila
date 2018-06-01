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
        public IErrorReporter ErrorAssigningToNonReassignableData()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression assign = Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b"));

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        assign
                    ))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningToNonReassignableData, assign));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorSwapNonReassignableValues()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("swap", "T", VarianceMode.None,
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("t", NameReference.Create("T"), NameReference.Create("a")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("a")), NameReference.Create("b")),
                        Assignment.CreateStatement(Dereference.Create(NameReference.Create("b")), NameReference.Create("t"))
                    ))
                    .Constraints(ConstraintBuilder.Create("T")
                        .SetModifier(env.Options.ReassignableModifier()))
                    .Parameters(FunctionParameter.Create("a", NameFactory.ReferenceTypeReference("T")),
                        FunctionParameter.Create("b", NameFactory.ReferenceTypeReference("T"))));


                VariableDeclaration decl = VariableDeclaration.CreateStatement("a", null, Nat8Literal.Create("2"));
                FunctionCall swap_call = FunctionCall.Create("swap", NameReference.Create("a"), NameReference.Create("b"));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        decl,
                        VariableDeclaration.CreateStatement("b", null, Nat8Literal.Create("17")),
                        // error: both values are const
                        swap_call,
                        Return.Create(ExpressionFactory.Sub("a", "b"))
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedMutabilityConstraint, swap_call.Name));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTransitiveMutabilityTypeInheritance()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Alien")
                    .SetModifier(EntityModifier.Mutable));

                root_ns.AddBuilder(TypeBuilder.Create(NameDefinition.Create("Container", "CCC", VarianceMode.None))
                    .SetModifier(EntityModifier.Base));

                // we should get error here because the parent should evaluate to mutable type, and current one is non-mutable
                NameReference parent_name = NameReference.Create("Container", NameReference.Create("Alien"));
                root_ns.AddBuilder(TypeBuilder.Create("Done")
                    .Parents(parent_name));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_name));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTransitiveMutabilityTypePassing()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Alien")
                    .SetModifier(EntityModifier.Mutable));

                // Option is a type which mutability is not decided until concrete type is given
                // here we pass mutable type so it should evaluate into mutable one 
                // and next it should create an error when passing to const type
                VariableDeclaration decl_bad = VariableDeclaration.CreateStatement("x",
                            NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(TypeMutability.ForceConst)),
                            ExpressionFactory.OptionEmpty("Alien", Memory.Heap));

                root_ns.AddBuilder(TypeBuilder.Create("Cool"));

                VariableDeclaration decl_ok = VariableDeclaration.CreateStatement("y",
                            NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(TypeMutability.ForceConst)),
                            ExpressionFactory.OptionEmpty("Cool", Memory.Heap));

                root_ns.AddBuilder(FunctionBuilder.Create(NameFactory.MainFunctionName, NameFactory.Nat8TypeReference(),
                    Block.CreateStatement(
                        decl_bad,
                        ExpressionFactory.Readout("x"),
                        decl_ok,
                        ExpressionFactory.Readout("y"),
                        Return.Create(Nat8Literal.Create("0"))
                        )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, decl_bad.InitValue));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCastingWithMutabilityChange()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Alien")
                    .SetModifier(EntityModifier.Mutable));

                // since Alien is mutable it looks like we try to shake off neutral flag
                IExpression bad_cast = ExpressionFactory.DownCast(NameReference.Create("x"),
                                NameFactory.PointerTypeReference(NameReference.Create("Alien")));
                ReinterpretType bad_reinterpret = bad_cast.DescendantNodes().WhereType<ReinterpretType>().Single();

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x",

                            NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(TypeMutability.ReadOnly)),
                            Undef.Create()),
                        VariableDeclaration.CreateStatement("c", null, bad_cast),
                        ExpressionFactory.Readout("c"),

                        VariableDeclaration.CreateStatement("ok", null,
                            ExpressionFactory.DownCast(NameReference.Create("x"),
                                NameFactory.PointerTypeReference(NameReference.Create(TypeMutability.ReadOnly, "Alien")))),
                        ExpressionFactory.Readout("ok")
                )));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, bad_reinterpret));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAbusingForcedConst()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
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
                        EntityModifier.Public | env.Options.ReassignableModifier()))
                        );

                FunctionCall mut_call = FunctionCall.Create(NameReference.CreateThised("f", "m", NameFactory.MutableName("violate")));
                IExpression assignment = Assignment.CreateStatement(NameReference.CreateThised("f", "m"), Undef.Create());
                root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                    .With(VariableDeclaration.CreateStatement("f",
                        NameFactory.PointerTypeReference(NameReference.Create(TypeMutability.ForceConst, "Mangler")),
                        Undef.Create(),
                        EntityModifier.Public))
                     .With(FunctionBuilder.Create("testing", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(
                            mut_call,
                            assignment
                        ))));



                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, mut_call));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, assignment));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ForcingConstIndirectly()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true }.SetMutability(mutability));
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
                        NameFactory.PointerTypeReference(NameReference.Create(TypeMutability.ForceConst, "Mangler")),
                        Undef.Create(),
                        EntityModifier.Public))
                        );

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ForcingConstDirectly()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Stone"));

                root_ns.AddBuilder(TypeBuilder.Create("Mutator", "M")
                    .SetModifier(EntityModifier.Mutable));

                root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                    .With(VariableDeclaration.CreateStatement("f",
                        NameFactory.PointerTypeReference(NameReference.Create(TypeMutability.ForceConst, "Mutator", NameReference.Create("Stone"))),
                        Undef.Create(),
                        EntityModifier.Public))
                        );

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorForcingConst()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                NameReference rhs_value = NameReference.Create("a");

                IExpression assign = Assignment.CreateStatement(NameReference.Create("x"), rhs_value);

                VariableDeclaration decl = VariableDeclaration.CreateStatement("x",
                                NameFactory.StringPointerTypeReference(TypeMutability.ForceConst), StringLiteral.Create("hi"),
                                env.Options.ReassignableModifier());
                root_ns.AddBuilder(FunctionBuilder.Create("innocent",
                        NameFactory.UnitTypeReference(),
                        Block.CreateStatement(
                            // this is ok, we are assigning literal here
                            decl,
                            VariableDeclaration.CreateStatement("a",
                                NameFactory.StringPointerTypeReference(TypeMutability.ForceMutable),
                                StringLiteral.Create("no")),
                            // this is not ok, we are assigning mutable to const
                            assign,
                            ExpressionFactory.Readout("x")
                            )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, rhs_value));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableMethodCallingMutable()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                FunctionCall call = FunctionCall.Create(NameReference.CreateThised(NameFactory.MutableName("mutator")));
                root_ns.AddBuilder(TypeBuilder.Create("Elka")
                    .SetModifier(EntityModifier.Mutable)
                    .With(FunctionBuilder.Create("innocent", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(call)))
                    .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .SetModifier(EntityModifier.Mutable)));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CallingMutableFromImmutableMethod, call));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingMutablesOnNeutral()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Elka")
                    .SetModifier(EntityModifier.Mutable)
                    .With(PropertyBuilder.CreateAutoFull(env.Options, "numi", NameFactory.Int64TypeReference(), null))
                    .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                        Block.CreateStatement())
                        .SetModifier(EntityModifier.Mutable)));

                FunctionCall call = FunctionCall.Create(NameReference.Create("x", NameFactory.MutableName("mutator")));
                IExpression assignment = Assignment.CreateStatement(NameReference.Create("x", "numi"), Int64Literal.Create("5"));
                root_ns.AddBuilder(FunctionBuilder.Create("foo",
                    NameFactory.UnitTypeReference(),
                    Block.CreateStatement(
                        // we can assign both mutable and immutable to neutral
                        VariableDeclaration.CreateStatement("x",
                            NameFactory.PointerTypeReference(NameReference.Create(TypeMutability.ReadOnly, "Elka")),
                            ExpressionFactory.HeapConstructor(NameReference.Create("Elka"))),
                        call,
                        assignment
                )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, call));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringNonMutableInstance, assignment));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableMethodAlteringData()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression assignment = Assignment.CreateStatement(NameReference.CreateThised("f"), Int64Literal.Create("5"));
                root_ns.AddBuilder(TypeBuilder.Create("Elka")
                    .SetModifier(EntityModifier.Mutable)
                    .With(VariableDeclaration.CreateStatement("f", NameFactory.Int64TypeReference(), null,
                        EntityModifier.Public | env.Options.ReassignableModifier()))
                    .With(FunctionBuilder.Create("mutator", NameFactory.UnitTypeReference(),
                        Block.CreateStatement(
                            assignment
                            ))));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AlteringCurrentInImmutableMethod, assignment));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableMethodInImmutableType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Property property = PropertyBuilder.Create(env.Options, "bar", NameFactory.Int64TypeReference())
                        .WithSetter(body: null);
                FunctionDefinition function = FunctionBuilder.CreateDeclaration("getMe", NameFactory.UnitTypeReference())
                        .SetModifier(EntityModifier.Mutable);
                root_ns.AddBuilder(TypeBuilder.Create("Whatever")
                    .SetModifier(EntityModifier.Abstract)
                    .With(property)
                    .With(function));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PropertySetterInImmutableType, property));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFunctionInImmutableType, function));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutabilityLaunderingOnReturn()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Return ret = Return.Create(NameReference.Create("coll"));
                root_ns.AddBuilder(FunctionBuilder.Create("laundering", "T", VarianceMode.None,
                   NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T")),
                   Block.CreateStatement(new IExpression[] {
                       ret
                   }))
                   .Parameters(FunctionParameter.Create("coll",
                        NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference("T", mutability: TypeMutability.ForceMutable)))));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, ret.Expr));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter AssigningToNeutral()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true, DebugThrowOnError = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Mutant")
                    .SetModifier(EntityModifier.Mutable));

                root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new[] {
                    // we can assign both mutable and immutable to neutral
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( TypeMutability.ReadOnly)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    ExpressionFactory.Readout("x"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( TypeMutability.ReadOnly)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Untouchable"))),
                    ExpressionFactory.Readout("y"),
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningNeutrals()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Mutant")
                    .SetModifier(EntityModifier.Mutable));

                root_ns.AddBuilder(TypeBuilder.Create("Untouchable"));

                NameReference init_value_x = NameReference.Create("x");
                NameReference init_value_y = NameReference.Create("y");
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( TypeMutability.ReadOnly)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Mutant"))),
                    VariableDeclaration.CreateStatement("xx",
                        NameFactory.PointerTypeReference(NameReference.Create("Mutant")),init_value_x),
                    ExpressionFactory.Readout("xx"),
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference( TypeMutability.ReadOnly)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Untouchable"))),
                    VariableDeclaration.CreateStatement("yy",
                        NameFactory.PointerTypeReference(NameReference.Create("Untouchable")),init_value_y),
                    ExpressionFactory.Readout("yy"),
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value_x));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, init_value_y));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningMutableToImmutable()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Mutable));

                IExpression mutable_init = ExpressionFactory.HeapConstructor(NameReference.Create("Bar"));

                VariableDeclaration decl = VariableDeclaration.CreateStatement("x",
                    NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference()),
                    mutable_init);

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new[] {
                    decl,
                    ExpressionFactory.Readout("x"),
                    // this is OK, we mark target as mutable type and we pass indeed mutable one
                    VariableDeclaration.CreateStatement("y",
                        NameFactory.PointerTypeReference(NameFactory.IObjectTypeReference(overrideMutability: TypeMutability.ForceMutable)),
                        ExpressionFactory.HeapConstructor(NameReference.Create("Bar"))),
                    ExpressionFactory.Readout("y"),
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TypeMismatch, mutable_init));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorImmutableTypeDefinition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Mutable));

                VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.Int64TypeReference(),
                    null, env.Options.ReassignableModifier() | EntityModifier.Public);
                VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("Bar"),
                    Undef.Create(), modifier: EntityModifier.Public);
                TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                   .With(decl1)
                   .With(decl2));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFieldInImmutableType, decl1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MutableFieldInImmutableType, decl2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter TransitiveMutabilityTypeDefinition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl = VariableDeclaration.CreateStatement("m", NameReference.Create("T"),
                    Undef.Create(), modifier: EntityModifier.Public);
                // we can declare type Point as non-mutable, because it will be mutable or not depending on T
                TypeDefinition point_type = root_ns.AddBuilder(TypeBuilder.Create("Point", "T")
                   .With(decl));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorViolatingConstConstraint()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
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
                        .SetModifier(EntityModifier.Const))
                   .With(field));

                // Bar is mutable type, so we cannot construct Point<Bar> since Point requires immutable type
                NameReference wrong_type = NameReference.Create("Point", NameReference.Create("Bar"));
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo", null,
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitTypeReference(),

                    Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameReference.Create("Point",NameReference.Create("Foo")), Undef.Create()),
                    VariableDeclaration.CreateStatement("y", wrong_type, Undef.Create()),
                    ExpressionFactory.Readout("x"),
                    ExpressionFactory.Readout("y"),
                })));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ViolatedMutabilityConstraint, wrong_type));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMutableGlobalVariables()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.StrictMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { GlobalVariables = true,
                    RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Mutable));

                VariableDeclaration decl2 = VariableDeclaration.CreateStatement("m", NameReference.Create("Bar"),
                    null, modifier: EntityModifier.Public);

                root_ns.AddNode(decl2);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalMutableVariable, decl2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReassignableGlobalVariables()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Bar")
                    .SetModifier(EntityModifier.Mutable));

                VariableDeclaration decl1 = VariableDeclaration.CreateStatement("r", NameFactory.Int64TypeReference(),
                    null, env.Options.ReassignableModifier() | EntityModifier.Public);

                root_ns.AddNode(decl1);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.GlobalMutableVariable, decl1));
            }

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorMixedInheritance()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Parent")
                    .SetModifier(EntityModifier.Base | EntityModifier.Mutable));

                NameReference parent_name = NameReference.Create("Parent");
                root_ns.AddBuilder(TypeBuilder.Create("Child")
                    .Parents(parent_name));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.InheritanceMutabilityViolation, parent_name));
            }

            return resolver;
        }
    }
}
