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
    public class Flow
    {
        [TestMethod]
        public IErrorReporter ErrorReadingUninitializedWithConditionalAssignment()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var decl = VariableDeclaration.CreateStatement("s", NameFactory.Int64TypeReference(), null, EntityModifier.Reassignable);
            NameReference var_ref = NameReference.Create("s");
            var if_assign = IfBranch.CreateIf(BoolLiteral.CreateFalse(),
                new[] { Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")) });
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    decl,
                    if_assign,
                    VariableDeclaration.CreateStatement("x", null, var_ref),
                    Assignment.CreateStatement(NameReference.Sink(),NameReference.Create("x"))
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingUninitializedWithConditionalBreak()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            NameReference var_ref = NameReference.Create("s");
            var if_break = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { LoopInterrupt.CreateBreak() });
            var loop = Loop.CreateFor(null, null, null, new IExpression[] {
                    if_break,
                    Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3"))
                });
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64TypeReference(), null, EntityModifier.Reassignable),
                    loop,
                    VariableDeclaration.CreateStatement("x", null, var_ref),
                    Assignment.CreateStatement(NameReference.Sink(),NameReference.Create("x"))
            })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDoubleConditionalLoopInterruption()
        {
            var collector = new ReportCollector();
            // initially there was a bug in merging "if" branches with interruptions
            // to make sure it is gone, we run the test twice with reversed commands
            foreach (bool reverse in new[] { false, true })
            {
                var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
                var root_ns = env.Root;

                NameReference var_ref = NameReference.Create("s");
                LoopInterrupt break_loop = LoopInterrupt.CreateBreak("outer");
                LoopInterrupt cont_loop = LoopInterrupt.CreateContinue();

                var if_double_jump = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { reverse ? break_loop : cont_loop },
                    IfBranch.CreateElse(new[] { reverse ? cont_loop : break_loop }));
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
                        Assignment.CreateStatement(NameReference.Sink(),NameReference.Create("m1"))
                    });
                // since step is executed after the body is executed, from its POV the variable can be read
                var outer_loop = Loop.CreateFor(NameDefinition.Create("outer"),
                        init: null,
                        condition: null,
                        step: new IExpression[] { VariableDeclaration.CreateStatement("m3", null, NameReference.Create("s")),
                                                  Assignment.CreateStatement(NameReference.Sink(),NameReference.Create("m3")) },
                        body: new IExpression[] {
                        inner_loop,
                        Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("44")),
                        VariableDeclaration.CreateStatement("m2",null,NameReference.Create("s")),
                        Assignment.CreateStatement(NameReference.Sink(),NameReference.Create("m2"))
                    });
                var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                    NameDefinition.Create("main"),
                    ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                    Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64TypeReference(), null,EntityModifier.Reassignable),
                    outer_loop,
                    Assignment.CreateStatement(NameReference.Sink(), var_ref)
                    })));

                var resolver = NameResolver.Create(env);

                Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.VariableNotInitialized, var_ref));
                Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.UnreachableCode, unreachable_assign));

                collector.Add(resolver);
            }

            return collector;
        }

        [TestMethod]
        public IErrorReporter ReadingInitializedAfterConditionalBreak()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;

            var if_break = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { LoopInterrupt.CreateBreak() });
            var loop = Loop.CreateFor(null, null, null, new IExpression[] {
                    VariableDeclaration.CreateStatement("b", NameFactory.Int64TypeReference(), null, EntityModifier.Reassignable),
                    if_break,
                    Assignment.CreateStatement(NameReference.Create("b"), Int64Literal.Create("5")),
                    ExpressionFactory.Readout("b"), // safe to read it because locally "b" is initialized
                });
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64TypeReference(), null, EntityModifier.Reassignable),
                    loop,
                    Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")),
                    ExpressionFactory.Readout("s")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ReadingConditionallyInitializedWithConditionalReturn()
        {
            // this one is correct, because in one branch we exit from function, in other we do the assignment

            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;

            var return_or_assign = IfBranch.CreateIf(BoolLiteral.CreateFalse(),
                new[] { Return.Create() },
                IfBranch.CreateElse(new[] { Assignment.CreateStatement(NameReference.Create("s"), Int64Literal.Create("3")) }));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] {
                    VariableDeclaration.CreateStatement("s", NameFactory.Int64TypeReference(), null,EntityModifier.Reassignable),
                    return_or_assign,
                    ExpressionFactory.Readout("s")
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ConditionalReturn()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateFalse(), new[] { Return.Create(Int64Literal.Create("5")) },
                    IfBranch.CreateElse(new[] { Return.Create(Int64Literal.Create("3")) }));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("main"),
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] {
                    if_ctrl,
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count());

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterReturn()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var dead_return = Return.Create(DoubleLiteral.Create("3.3"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.ReadRequired,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")),
                    dead_return
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(dead_return, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterBreak()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;

            var dead_step = ExpressionFactory.Readout("i");
            var loop = Loop.CreateFor(NameDefinition.Create("pool"),
                init: new[] { VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("5")) },
                condition: BoolLiteral.CreateTrue(),
                step: new[] { dead_step },
                body: new IExpression[] { LoopInterrupt.CreateBreak("pool") });

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    loop
                })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(dead_step, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableCodeAfterBreakSingleReport()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true  });
            var root_ns = env.Root;

            var dead_return = Return.Create();
            var dead_step = ExpressionFactory.Readout("i");
            var loop = Loop.CreateFor(NameDefinition.Create("pool"),
                init: new[] { VariableDeclaration.CreateStatement("i", null, Int64Literal.Create("5")) },
                condition: BoolLiteral.CreateTrue(),
                step: new[] { dead_step },
                body: new IExpression[] { LoopInterrupt.CreateBreak("pool"), dead_return });

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                        usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.CannotBeRead,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new[] {
                    loop
                })));

            var resolver = NameResolver.Create(env);

            // since the cause is the same, only one report is created
            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
            Assert.IsTrue(resolver.ErrorManager.Errors.Select(it => it.Node).Contains(dead_return)
                || resolver.ErrorManager.Errors.Select(it => it.Node).Contains(dead_step));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorMissingReturn()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var dead_return = Return.Create(DoubleLiteral.Create("3.3"));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                new[] { FunctionParameter.Create("x", NameFactory.Int64TypeReference(), Variadic.None, null, false,
                    usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.ReadRequired,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement()));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.MissingReturn, resolver.ErrorManager.Errors.Single().Code);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReturnOutsideFunction()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var ret = Return.Create(BoolLiteral.CreateTrue());

            root_ns.AddNode(ret);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.ReturnOutsideFunction, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(ret, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingMixedIf()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var cond = BoolLiteral.CreateTrue();
            var str_literal = DoubleLiteral.Create("3.3");
            var int_literal = Int64Literal.Create("5");

            var if_ctrl = IfBranch.CreateIf(cond, new[] { str_literal },
                IfBranch.CreateElse(new[] { int_literal }));

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, if_ctrl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingIfWithoutElse()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var cond = BoolLiteral.CreateTrue();
            var str_literal = DoubleLiteral.Create("3.3");

            var if_ctrl = IfBranch.CreateIf(cond, new[] { str_literal });

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, if_ctrl));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorNonBoolIfCondition()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var str_literal = DoubleLiteral.Create("3.3");

            var if_ctrl = IfBranch.CreateIf(str_literal, new[] { Int64Literal.Create("5") },
                IfBranch.CreateElse(new[] { Int64Literal.Create("5") }));

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(str_literal, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorNonBoolForCondition()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var str_literal = DoubleLiteral.Create("3.3");

            var loop = Loop.CreateFor(init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                condition: str_literal,
                step: new[] { ExpressionFactory.Readout("x") },
                body: new IExpression[] { });

            root_ns.AddNode(loop);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.TypeMismatch, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(str_literal, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ProperLoopBreaking()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                condition: BoolLiteral.CreateTrue(),
                step: null,
                body: new[] {
                    ExpressionFactory.Readout("x"),
                    LoopInterrupt.CreateBreak("foo") });

            root_ns.AddNode(loop);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnreachableStepLoopBreaking()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var step = ExpressionFactory.Readout("x");
            var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                condition: BoolLiteral.CreateTrue(),
                step: new[] { step },
                body: new[] { LoopInterrupt.CreateBreak("foo") });

            root_ns.AddNode(loop);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.UnreachableCode, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(step, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ReachableStepLoopContinue()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            var step = ExpressionFactory.Readout("x");
            var loop = Loop.CreateFor(NameDefinition.Create("foo"),
                init: new[] { VariableDeclaration.CreateStatement("x", null, Int64Literal.Create("5")) },
                condition: BoolLiteral.CreateTrue(),
                step: new[] { step },
                body: new[] { LoopInterrupt.CreateContinue("foo") });

            root_ns.AddNode(loop);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorBreakOutsideLoop()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var brk = LoopInterrupt.CreateBreak();
            var func_def_int = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"),
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),
                
                Block.CreateStatement(new IExpression[] { brk })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count());
            Assert.AreEqual(ErrorCode.LoopControlOutsideLoop, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(brk, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorMultipleElse()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var cond = BoolLiteral.CreateTrue();

            IfBranch if_else = IfBranch.CreateElse(new[] { Int64Literal.Create("5") },
                IfBranch.CreateElse(new[] { Int64Literal.Create("5") }));
            var if_ctrl = IfBranch.CreateIf(cond, new[] { Int64Literal.Create("5") }, if_else);

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.MiddleElseBranch, if_else));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorReadingOtherIfBlocks()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var wrong_name_ref = NameReference.Create("y");

            var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                new IExpression[] { VariableDeclaration.CreateStatement("y", null, BoolLiteral.CreateTrue()),
                                    NameReference.Create( "y")
                },
                    IfBranch.CreateElse(new[] { wrong_name_ref }));

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", null, if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ReferenceNotFound, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(wrong_name_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }
    }
}