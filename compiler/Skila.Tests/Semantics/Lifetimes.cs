using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;
using Skila.Language.Flow;
using Skila.Language.Tools;
using System.Linq;
using Skila.Language.Extensions;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Lifetimes
    {
        [TestMethod]
        public IErrorReporter ErrorEscapingReceivedReferenceFromGetter()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                Property property = PropertyBuilder.CreateReferential(env.Options, "meow",
                    () => NameFactory.IntNameReference())
                    .WithAutoField(Undef.Create())
                    .WithAutoGetter();

                root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                    .With(property));

                NameReference heap_get_ref = NameReference.Create("h", "meow");
                NameReference stack_get_ref = NameReference.Create("s", "meow");
                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("i", NameFactory.ReferenceNameReference(NameFactory.IntNameReference()),
                            IntLiteral.Create("0"), EntityModifier.Reassignable),
                        VariableDeclaration.CreateStatement("h", null, ExpressionFactory.HeapConstructor("Keeper")),
                        Block.CreateStatement(
                            VariableDeclaration.CreateStatement("s", null, ExpressionFactory.StackConstructor("Keeper")),
                            Assignment.CreateStatement(NameReference.Create("i"), heap_get_ref),
                            Assignment.CreateStatement(NameReference.Create("i"), stack_get_ref)
                            ),
                        ExpressionFactory.Readout("i")
                    )));


                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, heap_get_ref));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, stack_get_ref));
                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            }
            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorEscapingReceivedReferenceFromField()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Keeper")
                    .With(VariableDeclaration.CreateStatement("world", NameFactory.IntNameReference(), null, EntityModifier.Public)));

                NameReference heap_field_ref = NameReference.Create("h", "world");
                NameReference stack_field_ref = NameReference.Create("s", "world");
                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("i", NameFactory.ReferenceNameReference(NameFactory.IntNameReference()),
                            IntLiteral.Create("0"), EntityModifier.Reassignable),
                        VariableDeclaration.CreateStatement("h", null, ExpressionFactory.HeapConstructor("Keeper")),
                        Block.CreateStatement(
                            VariableDeclaration.CreateStatement("s", null, ExpressionFactory.StackConstructor("Keeper")),
                            Assignment.CreateStatement(NameReference.Create("i"), heap_field_ref),
                            Assignment.CreateStatement(NameReference.Create("i"), stack_field_ref)
                            ),
                        ExpressionFactory.Readout("i")
                    )));


                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, heap_field_ref));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, stack_field_ref));
                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            }
            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorEscapingReceivedReferenceFromFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("selector",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.ReferenceNameReference(NameFactory.IntNameReference()),

                    Block.CreateStatement(
                        ExpressionFactory.Readout("b"),
                        Return.Create(NameReference.Create("a"))
                    ))
                    .Parameters(
                        FunctionParameter.Create("a", NameFactory.ReferenceNameReference(NameFactory.IntNameReference())),
                        FunctionParameter.Create("b", NameFactory.ReferenceNameReference(NameFactory.IntNameReference()))));


                FunctionCall call = FunctionCall.Create("selector", IntLiteral.Create("2"), IntLiteral.Create("3"));

                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("h", NameFactory.ReferenceNameReference(NameFactory.IntNameReference()),
                            IntLiteral.Create("0"), EntityModifier.Reassignable),
                        Block.CreateStatement(
                            // error: the most alive reference the function can return is limited to this scope
                            // so it cannot be assigned to outer-scope variable
                            Assignment.CreateStatement(NameReference.Create("h"), call)
                            ),
                        ExpressionFactory.Readout("h")
                    )));


                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, call));
            }
            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorSettingPropertyReferenceField()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                // this is incorrect we use &Int in setter (OK) but we store this reference in a field too (bad)
                Property property = PropertyBuilder.Create(env.Options, "meow",
                    () => NameFactory.ReferenceNameReference(NameFactory.IntNameReference()))
                    .WithAutoField(Undef.Create(), EntityModifier.Reassignable)
                    .WithAutoSetter();

                root_ns.AddBuilder(TypeBuilder.Create("whatever")
                    .SetModifier(EntityModifier.Mutable)
                    .With(property));

                resolver = NameResolver.Create(env);

                var setter_assignment = property.Setter.UserBody.Instructions.Single().Cast<Assignment>();

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, setter_assignment.RhsValue));
            }
            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorStoredLocalReferenceEscapesFromFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                Return ret = Return.Create(NameReference.Create("o"));

                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.OptionNameReference(NameFactory.ReferenceNameReference(NameFactory.IntNameReference())),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("h", null, IntLiteral.Create("3")),
                        VariableDeclaration.CreateStatement("o", null,
                            ExpressionFactory.StackConstructor(
                                NameFactory.OptionNameReference(NameFactory.ReferenceNameReference(NameFactory.IntNameReference())),
                            NameReference.Create("h"))),
                        // error, we would effectively escape with reference to local variable
                        ret
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, ret.Expr));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorLocalVariableReferenceEscapesFromFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Hi")
                    .With(FunctionBuilder.Create("give", NameFactory.UnitNameReference(), Block.CreateStatement())));

                Return ret = Return.Create(NameReference.Create("h"));
                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceNameReference("Hi"),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("h", null, ExpressionFactory.StackConstructor("Hi")),
                        // invalid, we cannot return reference to local variable
                        ret
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, ret.Expr));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorLocalVariableReferenceEscapesFromScope()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                NameReference assignment_rhs = NameReference.Create("y");
                IExpression assignment = Assignment.CreateStatement(NameReference.Create("x"), assignment_rhs);

                root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()), null,
                            env.Options.ReassignableModifier()),
                        Block.CreateStatement(
                            VariableDeclaration.CreateStatement("y", NameFactory.Int64NameReference(), Int64Literal.Create("0")),
                            // escaping assignment, once we exit the scope we lose the source of the reference --> error
                            assignment
                            ),
                        ExpressionFactory.Readout("x")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, assignment_rhs));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReassignableReferenceField()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }
                    .SetMutability(mutability));
                var root_ns = env.Root;

                VariableDeclaration decl = VariableDeclaration.CreateStatement("f",
                        NameFactory.ReferenceNameReference(NameFactory.IntNameReference()),
                        Undef.Create(),
                        EntityModifier.Reassignable | EntityModifier.Public);

                root_ns.AddBuilder(TypeBuilder.Create("X")
                    .SetModifier(EntityModifier.Mutable)
                    .With(decl));

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ReferenceFieldCannotBeReassignable, decl));
                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        //[TestMethod]
        public IErrorReporter DEPRECATED_ErrorPersistentReferenceType()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                    GlobalVariables = true,
                    RelaxedMode = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                var decl1 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                    initValue: Undef.Create(), modifier: EntityModifier.Public);
                root_ns.AddNode(decl1);

                var decl2 = VariableDeclaration.CreateStatement("bar", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()),
                    initValue: Undef.Create(), modifier: EntityModifier.Static);

                var func_def_void = root_ns.AddBuilder(FunctionBuilder.Create(
                    "notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    decl2,
                    ExpressionFactory.Readout("bar")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.PersistentReferenceVariable, decl2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTemporaryValueReferenceEscapesFromScope()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                Int64Literal assignment_rhs = Int64Literal.Create("3");
                IExpression assignment = Assignment.CreateStatement(NameReference.Create("x"), assignment_rhs);

                FunctionDefinition func = FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("x", NameFactory.ReferenceNameReference(NameFactory.Int64NameReference()), null,
                            env.Options.ReassignableModifier()),
                        Block.CreateStatement(
                            // escaping assignment, once we exit the scope we lose the source of the reference --> error
                            assignment
                            ),
                        ExpressionFactory.Readout("x")
                    ));

                root_ns.AddNode(func);

                resolver = NameResolver.Create(env);

                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, assignment_rhs));
                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorTemporaryValueReferenceEscapesFromFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(TypeBuilder.Create("Hi")
                    .With(FunctionBuilder.Create("give", NameFactory.UnitNameReference(), Block.CreateStatement())));

                Return ret = Return.Create(ExpressionFactory.StackConstructor("Hi"));

                FunctionDefinition func = root_ns.AddBuilder(FunctionBuilder.Create("notimportant",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.ReferenceNameReference("Hi"),

                    Block.CreateStatement(
                        ret
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.EscapingReference, ret.Expr));
            }

            return resolver;
        }


    }
}