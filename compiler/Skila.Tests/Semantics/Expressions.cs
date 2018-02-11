using Microsoft.VisualStudio.TestTools.UnitTesting;
using Skila.Language;
using System.Linq;
using Skila.Language.Expressions;
using Skila.Language.Entities;
using Skila.Language.Builders;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using Skila.Language.Expressions.Literals;

namespace Skila.Tests.Semantics
{
    [TestClass]
    public class Expressions
    {
        [TestMethod]
        public IErrorReporter ErrorIsSameOnValues()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            Int64Literal value = Int64Literal.Create("3");
            root_ns.AddBuilder(FunctionBuilder.Create("foo",
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(
                    VariableDeclaration.CreateStatement("x", null, 
                        ExpressionFactory.HeapConstructor(NameFactory.Int64TypeReference(), Int64Literal.Create("2"))),
                    Return.Create(IsSame.Create(NameReference.Create("x"),value))
            )));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotUseValueExpression, value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDereferencingValue()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            Int64Literal value = Int64Literal.Create("3");
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create( Dereference.Create(value)),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DereferencingValue, value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorDiscardingNonFunctionCall()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            IExpression discard = ExpressionFactory.Readout("c");
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("c", null,Int64Literal.Create("3")),
                    discard,
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.DiscardingNonFunctionCall, discard));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorCastingToSet()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            NameReferenceUnion type_set = NameReferenceUnion.Create(
                NameFactory.PointerTypeReference(NameFactory.BoolTypeReference()),
                NameFactory.PointerTypeReference(NameFactory.Int64TypeReference()));
            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", NameFactory.ObjectTypeReference(), Undef.Create()),
                    VariableDeclaration.CreateStatement("c", null,ExpressionFactory.DownCast(NameReference.Create("x"), type_set)),
                    ExpressionFactory.Readout("c"),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.TestingAgainstTypeSet, type_set));

            return resolver;
        }
        [TestMethod]
        public IErrorReporter ErrorAddressingRValue()
        {
            var env = Environment.Create(new Options() { DiscardingAnyExpressionDuringTests = true });
            var root_ns = env.Root;

            Int64Literal int_literal = Int64Literal.Create("1");

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), null,
                ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                Block.CreateStatement(new[] {
                    VariableDeclaration.CreateStatement("x", null, AddressOf.CreatePointer(int_literal)),
                    VariableDeclaration.CreateStatement("y", null, AddressOf.CreateReference(Int64Literal.Create("2"))),
                    ExpressionFactory.Readout("x"),
                    ExpressionFactory.Readout("y"),
            })));


            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AddressingRValue, int_literal));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ReadingIfAsExpression()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var if_ctrl = IfBranch.CreateIf(BoolLiteral.CreateTrue(),
                new IExpression[] { Int64Literal.Create("5")
                },
                    IfBranch.CreateElse(new[] { Int64Literal.Create("7") }));

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), if_ctrl, EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(0, resolver.ErrorManager.Errors.Count);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUsingNonValue()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            NameReference non_value = NameFactory.Int64TypeReference();

            root_ns.AddNode(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), initValue: non_value,
                modifier: EntityModifier.Public));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.NoValueExpression, non_value));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorIgnoringFunctionResult()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.ReadRequired,
                NameFactory.DoubleTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(DoubleLiteral.Create("3.3")) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            root_ns.AddNode(Block.CreateStatement(new[] { call }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.ExpressionValueNotUsed, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningSimpleRValues()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getter"),
                null,
                ExpressionReadMode.ReadRequired,
                NameFactory.Int64TypeReference(),
                Block.CreateStatement(new[] { Return.Create(Int64Literal.Create("5")) })));

            FunctionCall call = FunctionCall.Create(NameReference.Create("getter"));
            NameReference func_ref = NameReference.Create("getter");
            root_ns.AddNode(Block.CreateStatement(new IExpression[] {
                VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()),
                // errors: assigning to r-value
                Assignment.CreateStatement(call,NameReference.Create("i")),
                Assignment.CreateStatement(func_ref,Undef.Create())
            }));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, call));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.AssigningRValue, func_ref));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorAssigningCompoundRValues()
        {
            var env = Environment.Create();
            var root_ns = env.Root;

            var point_type = root_ns.AddBuilder(TypeBuilder.Create("Point")
                .Modifier(EntityModifier.Mutable)
                .With(VariableDeclaration.CreateStatement("x", NameFactory.Int64TypeReference(), null,
                    EntityModifier.Public | EntityModifier.Reassignable)));
            var func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("getter"),
                null,
                ExpressionReadMode.ReadRequired,
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
        public IErrorReporter ErrorReadingFunctionVoidResult()
        {
            var env = Environment.Create(new Options() { GlobalVariables = true, TypelessVariablesDuringTests = true });
            var root_ns = env.Root;

            root_ns.AddBuilder(FunctionBuilder.Create(
                NameDefinition.Create("foo"), new[] {
                    FunctionParameter.Create("x", NameFactory.Int64TypeReference(), usageMode: ExpressionReadMode.CannotBeRead) },
                ExpressionReadMode.CannotBeRead,
                NameFactory.BoolTypeReference(),
                Block.CreateStatement(new[] {
                    Return.Create(BoolLiteral.CreateTrue()) })));

            root_ns.AddNode(VariableDeclaration.CreateStatement("i", NameFactory.Int64TypeReference(), Undef.Create()));
            var call = FunctionCall.Create(NameReference.Create("foo"), FunctionArgument.Create(NameReference.Create("i")));
            VariableDeclaration decl = VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(),
                call, EntityModifier.Public);
            root_ns.AddNode(decl);

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.CannotReadExpression, call));

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorUnusedExpression()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            var var_decl = VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(), Undef.Create());
            var var_ref = NameReference.Create("x");
            FunctionDefinition func_def = root_ns.AddBuilder(FunctionBuilder.Create(
                          NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                          ExpressionReadMode.OptionalUse,
                          NameFactory.BoolTypeReference(),
                          Block.CreateStatement(new IExpression[] {
                              var_decl,
                              var_ref,
                              Return.Create(BoolLiteral.CreateTrue())
                          })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(1, resolver.ErrorManager.Errors.Count);
            Assert.AreEqual(ErrorCode.ExpressionValueNotUsed, resolver.ErrorManager.Errors.Single().Code);
            Assert.AreEqual(var_ref, resolver.ErrorManager.Errors.Single().Node);

            return resolver;
        }

        [TestMethod]
        public IErrorReporter ErrorSelfAssignment()
        {
            var env = Environment.Create();
            var root_ns = env.Root;
            var system_ns = env.SystemNamespace;

            root_ns.AddBuilder(TypeBuilder.Create("Oint")
                .Modifier(EntityModifier.Mutable)
                .With(Property.Create("x", NameFactory.Int64TypeReference(),
                    new[] { Property.CreateAutoField(NameFactory.Int64TypeReference(), null, EntityModifier.Reassignable) },
                    new[] { Property.CreateAutoGetter(NameFactory.Int64TypeReference()) },
                    new[] { Property.CreateAutoSetter(NameFactory.Int64TypeReference()) })));

            var var_decl = VariableDeclaration.CreateStatement("x", NameFactory.BoolTypeReference(),
                Undef.Create(), EntityModifier.Reassignable);

            IExpression assign_var = Assignment.CreateStatement(NameReference.Create("x"), NameReference.Create("x"));
            IExpression assign_prop = Assignment.CreateStatement(NameReference.Create("a", "x"), NameReference.Create("a", "x"));

            root_ns.AddBuilder(FunctionBuilder.Create(
                          NameDefinition.Create("foo"), Enumerable.Empty<FunctionParameter>(),
                          ExpressionReadMode.OptionalUse,
                NameFactory.UnitTypeReference(),

                          Block.CreateStatement(new IExpression[] {
                              var_decl,
                              assign_var,
                              VariableDeclaration.CreateStatement("a",null,ExpressionFactory.StackConstructor("Oint")),
                              VariableDeclaration.CreateStatement("b",null,ExpressionFactory.StackConstructor("Oint")),
                              Assignment.CreateStatement(NameReference.Create("a","x"),NameReference.Create("b","x")),
                              assign_prop,
                          })));

            var resolver = NameResolver.Create(env);

            Assert.AreEqual(2, resolver.ErrorManager.Errors.Count);
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelfAssignment, assign_var));
            Assert.IsTrue(resolver.ErrorManager.HasError(ErrorCode.SelfAssignment, assign_prop));

            return resolver;
        }
    }
}