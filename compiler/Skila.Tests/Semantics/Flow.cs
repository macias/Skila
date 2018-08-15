using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Flow : ITest
    {
        [TestMethod]
        public IErrorReporter CombiningBranchedInitialization()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                // this test is mimics how optional declarations works with conditions

                // conditionally assigning variable
                System.Func<string, IfBranch> branched_init = name => IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                            new[]
                            {
                            Assignment.CreateStatement(NameReference.Create(name),BoolLiteral.CreateTrue()),
                            BoolLiteral.CreateTrue(),
                            },
                            IfBranch.CreateElse(BoolLiteral.CreateFalse()));

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                            VariableDeclaration.CreateStatement("a", NameFactory.BoolNameReference(), null),
                            VariableDeclaration.CreateStatement("b", NameFactory.BoolNameReference(), null),

                        IfBranch.CreateIf( ExpressionFactory.And(branched_init("a"), branched_init("b")), new[] {
                        // at his point both variables should be initialized
                         ExpressionFactory.Readout("a"),
                         ExpressionFactory.Readout("b"),
                        })
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorPostponedInitialization()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                IExpression second_init_x = Assignment.CreateStatement(NameReference.Create("x"), IntLiteral.Create("5"));
                IExpression second_init_y = Assignment.CreateStatement(NameReference.Create("y"), IntLiteral.Create("5"));
                IExpression second_init_z_1 = Assignment.CreateStatement(NameReference.Create("z"), IntLiteral.Create("5"));
                IExpression second_init_z_2 = Assignment.CreateStatement(NameReference.Create("z"), IntLiteral.Create("5"));

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        // the point is those variables are not reassingable, yet we don't initialize them on declaration
                        // but we make incorrect initializations in this test
                        VariableDeclaration.CreateStatement("x", NameFactory.IntNameReference(), null),
                        Assignment.CreateStatement(NameReference.Create("x"), IntLiteral.Create("5")),
                        second_init_x,

                        VariableDeclaration.CreateStatement("y", NameFactory.IntNameReference(), null),
                        IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                             ExpressionFactory.Nop,
                            IfBranch.CreateElse(
                                Assignment.CreateStatement(NameReference.Create("y"), IntLiteral.Create("5")))),

                        second_init_y,

                        VariableDeclaration.CreateStatement("z", NameFactory.IntNameReference(), null),

                        Loop.CreateFor(null, BoolLiteral.CreateTrue(), null, new[] {
                        second_init_z_1,
                        }),

                        second_init_z_2,

                         ExpressionFactory.Readout("x"),
                         ExpressionFactory.Readout("y"),
                         ExpressionFactory.Readout("z")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(4, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, second_init_x));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, second_init_y));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, second_init_z_1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReassignReadOnlyVariable, second_init_z_2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter PostponedInitialization()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {

                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                    DebugThrowOnError = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                        // the point is those variables are not reassingable, yet we don't initialize them on declaration
                        VariableDeclaration.CreateStatement("x", NameFactory.IntNameReference(), null),
                        Assignment.CreateStatement(NameReference.Create("x"), IntLiteral.Create("5")),

                        VariableDeclaration.CreateStatement("y", NameFactory.IntNameReference(), null),
                        IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                            Assignment.CreateStatement(NameReference.Create("y"), IntLiteral.Create("5")),
                            IfBranch.CreateElse(
                                Assignment.CreateStatement(NameReference.Create("y"), IntLiteral.Create("5")))),

                        // double loop to check if our computing nested repeated flow for assignment works correctly
                        Loop.CreateFor(null, BoolLiteral.CreateTrue(), null, new[] {
                        Loop.CreateFor(null, BoolLiteral.CreateTrue(), null, new[] {
                            VariableDeclaration.CreateStatement("z", NameFactory.IntNameReference(), null),
                            Assignment.CreateStatement(NameReference.Create("z"), IntLiteral.Create("5")),
                             ExpressionFactory.Readout("z")
                        }),
                        }),

                         ExpressionFactory.Readout("x"),
                         ExpressionFactory.Readout("y")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorLinearFlowAfterOptionalDeclaration()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference not_initialized = NameReference.Create("m");

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntNameReference(),
                    Block.CreateStatement(
                            VariableDeclaration.CreateStatement("o", null,  ExpressionFactory.OptionEmpty(NameFactory.IntNameReference())),

                             ExpressionFactory.Readout( ExpressionFactory.OptionalDeclaration("m", null, NameReference.Create("o"))),

                            // at this point `m` could be not initialized
                            Return.Create(not_initialized)
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorLinearFlowAfterOptionalAssignment()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference not_initialized = NameReference.Create("m");

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.IntNameReference(),
                    Block.CreateStatement(
                            VariableDeclaration.CreateStatement("m", NameFactory.IntNameReference(), null, env.Options.ReassignableModifier()),

                            VariableDeclaration.CreateStatement("o", null,  ExpressionFactory.OptionEmpty(NameFactory.IntNameReference())),

                             ExpressionFactory.Readout( ExpressionFactory.OptionalAssignment(NameReference.Create("m"), NameReference.Create("o"))),

                            // at this point `m` could be not initialized
                            Return.Create(not_initialized)
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorExtendedAssignmentTracking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                // this test is mimics how optional assignment works with conditions
                // we have nested `if` which sends outside true/false depending if assigment was succesful
                // our assign tracker should use that hint and detect which variables are initialized
                NameReference not_initialized1 = NameReference.Create("n");
                NameReference not_initialized2 = NameReference.Create("m");
                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                            VariableDeclaration.CreateStatement("n", NameFactory.BoolNameReference(), null, env.Options.ReassignableModifier()),
                            VariableDeclaration.CreateStatement("m", NameFactory.BoolNameReference(), null, env.Options.ReassignableModifier()),

                        // main if
                        IfBranch.CreateIf(
                        // mega condition
                        IfBranch.CreateIf(
                             ExpressionFactory.And(
                            VariableDeclaration.CreateExpression("temp1", null, BoolLiteral.CreateTrue()),
                            VariableDeclaration.CreateExpression("temp2", null, BoolLiteral.CreateTrue())),
                            new[]
                            {
                            Assignment.CreateStatement("n","temp1"),
                            Assignment.CreateStatement("m","temp2"),
                            BoolLiteral.CreateTrue(),
                            },
                            IfBranch.CreateElse(BoolLiteral.CreateFalse())),
                        // -- end of mega condition
                         ExpressionFactory.Nop,
                        // in general we cannot guarantee we can read it in `else` branch
                        IfBranch.CreateElse( ExpressionFactory.Readout(not_initialized1))),
                        // -- end of main if

                        // `else` branch is live, so `m` can be still uninitialized
                         ExpressionFactory.Readout(not_initialized2)

                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ExtendedAssignmentTracking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                // this test is mimics how optional assignment works with conditions
                // we have nested `if` which sends outside true/false depending if assigment was succesful
                // our assign tracker should use that hint and detect which variables are initialized
                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(
                            VariableDeclaration.CreateStatement("a", NameFactory.BoolNameReference(), null, env.Options.ReassignableModifier()),
                            VariableDeclaration.CreateStatement("b", NameFactory.BoolNameReference(), null, env.Options.ReassignableModifier()),

                        // main if
                        IfBranch.CreateIf(
                        // mega condition
                        IfBranch.CreateIf(
                             ExpressionFactory.And(
                            VariableDeclaration.CreateExpression("temp1", null, BoolLiteral.CreateTrue()),
                            VariableDeclaration.CreateExpression("temp2", null, BoolLiteral.CreateTrue())),
                            new[]
                            {
                            Assignment.CreateStatement("a","temp1"),
                            Assignment.CreateStatement("b","temp2"),
                            BoolLiteral.CreateTrue(),
                            },
                            IfBranch.CreateElse(BoolLiteral.CreateFalse())),
                        // -- end of mega condition
                         ExpressionFactory.Readout("a"),
                        // making `else` a dead branch
                        IfBranch.CreateElse( ExpressionFactory.GenericThrow())),
                        // -- end of main if

                        // `b` is initialized because `else` is a thrower, so the only way is going through `then`
                        // and `then` is activated only when all variables are initialized
                         ExpressionFactory.Readout("b")

                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter BranchedAssignmentTracking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                // this test was added when working on parallel assignments, it is simplified version of code
                // which has at least 2 such assignments, when there was only 1 everything worked, but after 
                // adding another one there was (incorrect) error reported that the second temporary variable was not initialized
                // the cause for this error was the second `if-then` started tracking assignments after first `if-then` 

                var env = Language.Environment.Create(new Options()
                {
                    DebugThrowOnError = true,
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("maiden",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),
                    Block.CreateStatement(

                        IfBranch.CreateIf(VariableDeclaration.CreateExpression("temp1", null, BoolLiteral.CreateTrue()),
                             ExpressionFactory.Readout(NameReference.Create("temp1"))),

                        IfBranch.CreateIf(VariableDeclaration.CreateExpression("temp2", null, BoolLiteral.CreateTrue()),
                             ExpressionFactory.Readout(NameReference.Create("temp2")))

                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter DeclarationsOnTheFly()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                    DebugThrowOnError = true
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "f",
                    NameFactory.BoolNameReference(),

                    Block.CreateStatement(Return.Create(BoolLiteral.CreateFalse()))));

                root_ns.AddBuilder(FunctionBuilder.Create(
                    "testing",
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", NameFactory.IntNameReference(), Undef.Create(), env.Options.ReassignableModifier()),

                        // if (x := 3) == 5 then a = x;
                        IfBranch.CreateIf( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                            VariableDeclaration.CreateExpression("x", null, IntLiteral.Create("3"))),
                            Assignment.CreateStatement("a", "x")),

                        // if (x := 3) == 5 then NOP else a = x;
                        IfBranch.CreateIf( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                            VariableDeclaration.CreateExpression("w", null, IntLiteral.Create("3"))),
                             ExpressionFactory.Nop, IfBranch.CreateElse(Assignment.CreateStatement("a", "w"))),

                           // if (y := 3) == 5 and f() then a = y;
                           IfBranch.CreateIf( ExpressionFactory.And( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("y", null, IntLiteral.Create("3"))),
                               FunctionCall.Create(NameReference.Create("f"))),
                               Assignment.CreateStatement("a", "y")),

                           // if (x := 3) == 5 and f() then NOP else a = x;
                           IfBranch.CreateIf( ExpressionFactory.And( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("r", null, IntLiteral.Create("3"))),
                               FunctionCall.Create(NameReference.Create("f"))),
                                ExpressionFactory.Nop,
                               IfBranch.CreateElse(Assignment.CreateStatement("a", "r"))),

                           // if f() and (z := 3) == 5 then a = z;
                           IfBranch.CreateIf( ExpressionFactory.And(FunctionCall.Create(NameReference.Create("f")),
                                ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("z", null, IntLiteral.Create("3")))),
                               Assignment.CreateStatement("a", "z")),

                           // if (x := 3) == 5 or f() then NOP else a = x;
                           IfBranch.CreateIf( ExpressionFactory.Or( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("u", null, IntLiteral.Create("3"))),
                               FunctionCall.Create(NameReference.Create("f"))),
                                ExpressionFactory.Nop,
                               IfBranch.CreateElse(Assignment.CreateStatement("a", "u"))),

                           // if (x := 3) == 5 or f() then a = x;
                           IfBranch.CreateIf( ExpressionFactory.Or( ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("q", null, IntLiteral.Create("3"))),
                               FunctionCall.Create(NameReference.Create("f"))),
                               Assignment.CreateStatement("a", "q")),

                           // if f() or (x := 3) == 5 then a = x;
                           IfBranch.CreateIf( ExpressionFactory.Or(FunctionCall.Create(NameReference.Create("f")),
                                ExpressionFactory.IsEqual(IntLiteral.Create("5"),
                               VariableDeclaration.CreateExpression("v", null, IntLiteral.Create("3")))),
                                ExpressionFactory.Nop,
                               IfBranch.CreateElse(Assignment.CreateStatement("a", "v"))),

                         ExpressionFactory.Readout("a")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDeclarationsOnTheFly()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                // this test was added because we noticed that despite `if` creates its own scope
                // variable created in first condition somehow leaked to the other `if` 
                // compiler didn't report the variable is not initialized
                var env = Environment.Create(new Options()
                {
                    DiscardingAnyExpressionDuringTests = true,
                }.SetMutability(mutability));
                var root_ns = env.Root;

                root_ns.AddBuilder(FunctionBuilder.Create("f", NameFactory.BoolNameReference(),
                    Block.CreateStatement(Return.Create(BoolLiteral.CreateFalse()))));

                const string reused_var_name = "xxx";
                IfBranch if_branch1 = IfBranch.CreateIf( ExpressionFactory.IsEqual(IntLiteral.Create("2"),
                            VariableDeclaration.CreateExpression(reused_var_name, null, IntLiteral.Create("1"))),
                             ExpressionFactory.Readout(reused_var_name));

                NameReference not_initialized1 = NameReference.Create(reused_var_name);
                NameReference not_initialized2 = NameReference.Create("yyy");

                root_ns.AddBuilder(FunctionBuilder.Create("testing",
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(
                        VariableDeclaration.CreateStatement("a", NameFactory.IntNameReference(), Undef.Create(), env.Options.ReassignableModifier()),

                        // the point of this `if` is to introduce variable in condition is such way
                        // the is initialized without doubt
                        if_branch1,

                        // at this point our variable is gone, because `if` scope is removed

                        // if f() and (xxx := 3)==5 then NOP else a = xxx;
                        // so when we reach `else` branch `xxx` might be not initialized because `f` failed first
                        IfBranch.CreateIf( ExpressionFactory.And(FunctionCall.Create(NameReference.Create("f")),
                             ExpressionFactory.IsEqual(IntLiteral.Create("555"),
                            VariableDeclaration.CreateExpression(reused_var_name, null, IntLiteral.Create("333")))),
                             ExpressionFactory.Nop,
                            // when writing this test `xxx` was reported to be initialized
                            IfBranch.CreateElse(Assignment.CreateStatement(NameReference.Create("a"), not_initialized1))),

                        // if f() or (yyy := 3)==5 then a = yyy;
                        IfBranch.CreateIf( ExpressionFactory.Or(FunctionCall.Create(NameReference.Create("f")),
                             ExpressionFactory.IsEqual(IntLiteral.Create("555"),
                            VariableDeclaration.CreateExpression("yyy", null, IntLiteral.Create("333")))),
                            Assignment.CreateStatement(NameReference.Create("a"), not_initialized2)),

                         ExpressionFactory.Readout("a")
                    )));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized1));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, not_initialized2));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingUninitializedWithConditionalAssignment()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var decl = VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null, env.Options.ReassignableModifier());
                NameReference var_ref = NameReference.Create("s");
                var if_assign = IfBranch.CreateIf(BoolLiteral.CreateFalse(),
                    new[] { Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")) });
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    decl,
                    if_assign,
                    VariableDeclaration.CreateStatement("x", null, var_ref),
                     ExpressionFactory.Readout("x")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingUninitializedWithConditionalBreak()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                NameReference var_ref = NameReference.Create("s");
                var if_break = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { LoopInterrupt.CreateBreak() });
                var loop = Loop.CreateFor(null, null, null, new IExpression[] {
                    if_break,
                    Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3"))
                });
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null, env.Options.ReassignableModifier()),
                    loop,
                    VariableDeclaration.CreateStatement("x", null, var_ref),
                     ExpressionFactory.Readout("x")
                })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDoubleConditionalLoopInterruption()
        {
            NameResolver resolver = null;
            var collector = new ReportCollector();
            foreach (var mutability in Options.AllMutabilityModes)
            {
                // initially there was a bug in merging "if" branches with interruptions
                // to make sure it is gone, we run the test twice with reversed commands
                foreach (bool reverse_order in new[] { false, true })
                {
                    var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                        AllowInvalidMainResult = true }.SetMutability(mutability));
                    var root_ns = env.Root;

                    NameReference var_ref = NameReference.Create("s");
                    LoopInterrupt break_outer_loop = LoopInterrupt.CreateBreak("outer");
                    LoopInterrupt cont_inner_loop = LoopInterrupt.CreateContinue();

                    var if_double_jump = IfBranch.CreateIf(BoolLiteral.CreateFalse(),
                        new[] { reverse_order ? break_outer_loop : cont_inner_loop },
                        IfBranch.CreateElse(new[] { reverse_order ? cont_inner_loop : break_outer_loop }));
                    IExpression unreachable_assign = Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3"));
                    // we interrupt this loop with "continue", so step is reachable
                    var inner_loop = Loop.CreateFor(
                            init: null,
                            condition: null,
                            step: new IExpression[] { Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("5")) },
                            body: new IExpression[] {
                        if_double_jump,
                        unreachable_assign,
                        VariableDeclaration.CreateStatement("m1",null,NameReference.Create("s")),
                         ExpressionFactory.Readout("m1")
                        });
                    // since step is executed after the body is executed, from its POV the variable can be read
                    var outer_loop = Loop.CreateFor(NameDefinition.Create("outer"),
                            init: null,
                            condition: null,
                            step: new IExpression[] { VariableDeclaration.CreateStatement("m3", null, NameReference.Create("s")),
                               ExpressionFactory.Readout("m3") },
                            body: new IExpression[] {
                        inner_loop,
                        // this assigment is skipped when we jump out of this (outer) loop
                        // or it is executed, in first case loop-step is not executed, in the second -- it is
                        Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("44")),
                        VariableDeclaration.CreateStatement("m2",null,NameReference.Create("s")),
                         ExpressionFactory.Readout("m2")
                        });
                    var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                        "main",
                        ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                        Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null,env.Options.ReassignableModifier()),
                    outer_loop,
                     ExpressionFactory.Readout(var_ref)
                        })));

                    resolver = NameResolver.Create(env);

                    Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                    Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));
                    Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UnreachableCode, unreachable_assign));

                    collector.Add(resolver);
                }
            }

            return collector;
        }

        [TestMethod]
        public IErrorReporter ReadingInitializedAfterConditionalBreak()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var if_break = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { LoopInterrupt.CreateBreak() });
                var loop = Loop.CreateFor(null, null, null, new IExpression[] {
                    VariableDeclaration.CreateStatement("b", NameFactory.Int64NameReference(), null, env.Options.ReassignableModifier()),
                    if_break,
                    Assignment.CreateStatement(NameReference.Create("b"), Int64Literal.Create("5")),
                     ExpressionFactory.Readout("b"), // safe to read it because locally "b" is initialized
                });
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null, env.Options.ReassignableModifier()),
                    loop,
                    Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")),
                     ExpressionFactory.Readout("s")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ReadingConditionallyInitializedWithConditionalReturn()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                // this one is correct, because in one branch we exit from function, in other we do the assignment

                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true,
                    AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var return_or_assign = IfBranch.CreateIf(BoolLiteral.CreateFalse(),
                    new[] { Return.Create() },
                    IfBranch.CreateElse(new[] { Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")) }));
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64NameReference(), null,env.Options.ReassignableModifier()),
                    return_or_assign,
                     ExpressionFactory.Readout("s")
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConditionalReturn()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { AllowInvalidMainResult = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { Return.Create(Int64Literal.Create("5")) },
                        IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("3")) }));
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "main",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.Int64NameReference(),
                    Block.CreateStatement(new[] {
                    if_ctrl,
                    })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterReturn()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var dead_return = Return.Create(RealLiteral.Create("3.3"));
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement(new[] {
                    Return.Create(RealLiteral.Create("3.3")),
                    dead_return
                    })).Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(dead_return, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterBreak()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var dead_step =  ExpressionFactory.Readout("i");
                var loop = Loop.CreateFor(NameDefinition.Create("pool"),
                    init: new[] { VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("5")) },
                    condition: BoolLiteral.CreateTrue(),
                    step: new[] { dead_step },
                    body: new IExpression[] { LoopInterrupt.CreateBreak("pool") });

                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    loop
                    })).Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UnreachableCode, dead_step));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterBreakSingleReport()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var dead_return = Return.Create();
                var dead_step =  ExpressionFactory.Readout("i");
                var loop = Loop.CreateFor(NameDefinition.Create("pool"),
                    init: new[] { VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("5")) },
                    condition: BoolLiteral.CreateTrue(),
                    step: new[] { dead_step },
                    body: new IExpression[] { LoopInterrupt.CreateBreak("pool"), dead_return });

                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.CannotBeRead,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new[] {
                    loop
                    })).Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                            usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                // since the cause is the same, only one report is created
                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
                Assert.IsTrue(resolver.ErrorManager.Errors.Select(it => it.Node).Contains(dead_return)
                    || resolver.ErrorManager.Errors.Select(it => it.Node).Contains(dead_step));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMissingReturn()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var dead_return = Return.Create(RealLiteral.Create("3.3"));
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.ReadRequired,
                    NameFactory.RealNameReference(),
                    Block.CreateStatement())
                    .Parameters(FunctionParameter.Create("x", NameFactory.Int64NameReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead)));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.MissingReturn, resolver.ErrorManager.Errors.Single().Code);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReturnOutsideFunction()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var ret = Return.Create(BoolLiteral.CreateTrue());

                root_ns.AddNode(ret);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(ErrorCode.ReturnOutsideFunction, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(ret, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingMixedIf()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var cond = BoolLiteral.CreateTrue();
                var str_literal = RealLiteral.Create("3.3");
                var int_literal = Int64Literal.Create("5");

                var if_ctrl = IfBranch.CreateIf(cond, new[] { str_literal },
                    IfBranch.CreateElse(new[] { int_literal }));

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, if_ctrl));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingIfWithoutElse()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var cond = BoolLiteral.CreateTrue();
                var str_literal = RealLiteral.Create("3.3");

                var if_ctrl = IfBranch.CreateIf(cond, new[] { str_literal });

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, if_ctrl));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNonBoolIfCondition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var str_literal = RealLiteral.Create("3.3");

                var if_ctrl = IfBranch.CreateIf(str_literal, new[] { Int64Literal.Create("5") },
                    IfBranch.CreateElse(new[] { Int64Literal.Create("5") }));

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), if_ctrl, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(str_literal, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }
        [TestMethod]

        public IErrorReporter ErrorNonBoolForCondition()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var str_literal = RealLiteral.Create("3.3");

                var loop = Loop.CreateFor(init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                    condition: str_literal,
                    step: new[] {  ExpressionFactory.Readout("x") },
                    body: new IExpression[] { });

                root_ns.AddNode(loop);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(str_literal, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ProperLoopBreaking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                    init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                    condition: BoolLiteral.CreateTrue(),
                    step: null,
                    body: new[] {
                     ExpressionFactory.Readout("x"),
                    LoopInterrupt.CreateBreak("foo") });

                root_ns.AddNode(loop);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableStepLoopBreaking()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var step =  ExpressionFactory.Readout("x");
                var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                    init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                    condition: BoolLiteral.CreateTrue(),
                    step: new[] { step },
                    body: new[] { LoopInterrupt.CreateBreak("foo") });

                root_ns.AddNode(loop);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(step, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ReachableStepLoopContinue()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var step =  ExpressionFactory.Readout("x");
                var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                    init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                    condition: BoolLiteral.CreateTrue(),
                    step: new[] { step },
                    body: new[] { LoopInterrupt.CreateContinue("foo") });

                root_ns.AddNode(loop);

                resolver = NameResolver.Create(env);

                Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorBreakOutsideLoop()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { }.SetMutability(mutability));
                var root_ns = env.Root;

                var brk = LoopInterrupt.CreateBreak();
                var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                    "foo",
                    ExpressionReadMode.OptionalUse,
                    NameFactory.UnitNameReference(),

                    Block.CreateStatement(new IExpression[] { brk })));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
                Assert.AreEqual(ErrorCode.LoopControlOutsideLoop, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(brk, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMultipleElse()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var cond = BoolLiteral.CreateTrue();

                IfBranch if_else = IfBranch.CreateElse(new[] { Int64Literal.Create("5") },
                    IfBranch.CreateElse(new[] { Int64Literal.Create("5") }));
                var if_ctrl = IfBranch.CreateIf(cond, new[] { Int64Literal.Create("5") }, if_else);

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64NameReference(), if_ctrl, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MiddleElseBranch, if_else));
            }

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingOtherIfBlocks()
        {
            NameResolver resolver = null;
            foreach (var mutability in Options.AllMutabilityModes)
            {
                var env = Environment.Create(new Options() { GlobalVariables = true, RelaxedMode = true }.SetMutability(mutability));
                var root_ns = env.Root;

                var wrong_name_ref = NameReference.Create("y");

                var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                    new IExpression[] { VariableDeclaration.CreateStatement("y", null, BoolLiteral.CreateTrue()),
                                    NameReference.Create( "y")
                    },
                        IfBranch.CreateElse(new[] { wrong_name_ref }));

                root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

                resolver = NameResolver.Create(env);

                Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
                Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);
                Assert.AreEqual(wrong_name_ref, resolver.ErrorManager.Errors.Single().Node);
            }

            return resolver;
        }
    }
}