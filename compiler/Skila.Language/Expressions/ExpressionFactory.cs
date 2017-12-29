using Skila.Language.Builders;
using Skila.Language.Entities;
using Skila.Language.Flow;
using System.Linq;

namespace Skila.Language.Expressions
{
    public static class ExpressionFactory
    {
        public static Block BodyReturnUndef()
        {
            return Block.CreateStatement(Return.Create(Undef.Create()));
        }
        public static TypeBuilder WithEquatableEquals(this TypeBuilder builder,EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.EqualOperator),
                                            ExpressionReadMode.ReadRequired, NameFactory.BoolTypeReference(),
                                            Block.CreateStatement(new[] {
                        // let obj = cmp cast? Int
                        VariableDeclaration.CreateStatement("obj",null,ExpressionFactory.DownCast(NameReference.Create("cmp"),
                            NameFactory.ReferenceTypeReference(builder.CreateTypeNameReference()))),
                        // if not obj.hasValue then throw
                        ExpressionFactory.IfOptionEmpty(NameReference.Create("obj"),
                            ExpressionFactory.GenericThrow()),
                        // return this==obj.value
                        Return.Create(ExpressionFactory.IsEqual(NameReference.Create(NameFactory.ThisVariableName),
                            ExpressionFactory.OptionValue(NameReference.Create("obj")))),
                                            }))
                                            .Modifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceTypeReference(NameFactory.EquatableTypeReference()))));
        }
        public static TypeBuilder WithComparableCompare(this TypeBuilder builder, EntityModifier modifier = null)
        {
            return builder.With(FunctionBuilder.Create(NameDefinition.Create(NameFactory.ComparableCompare),
                                            ExpressionReadMode.ReadRequired, NameFactory.OrderingTypeReference(),
                                            Block.CreateStatement(new[] {
                        // let obj = cmp cast? Int
                        VariableDeclaration.CreateStatement("obj",null,ExpressionFactory.DownCast(NameReference.Create("cmp"),
                            NameFactory.ReferenceTypeReference(builder.CreateTypeNameReference()))),
                        // if not obj.hasValue then return false
                        ExpressionFactory.IfOptionEmpty(NameReference.Create("obj"),ExpressionFactory.GenericThrow()),
                        // return this.compare(obj.value)
                        Return.Create(FunctionCall.Create(NameReference.CreateThised(NameFactory.ComparableCompare),
                            ExpressionFactory.OptionValue(NameReference.Create("obj")))),
                                            }))
                                            .Modifier(EntityModifier.Override | modifier)
                                            .Parameters(FunctionParameter.Create("cmp",
                                                NameFactory.ReferenceTypeReference(NameFactory.ComparableTypeReference()))));
        }
        public static FunctionCall BaseInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.CreateBaseInitReference(), arguments);
        }

        public static FunctionCall ThisInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.Create(NameFactory.ThisVariableName, NameFactory.InitConstructorName),
                arguments);
        }
        public static IExpression DownCast(IExpression lhs, INameReference rhsTypeName)
        {
            // if the expression is not of the given type we get null
            // if it is the runtime type IS PRESERVED
            // say you have statically types object
            // x *Object
            // and in runtime x is Orange
            // if you cast it to Vehicle you will get null, when you cast it to Fruit you will get Orange (sic!)
            IExpression condition = IsType.Create(lhs, rhsTypeName);
            IExpression success = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName),
                FunctionArgument.Create(ReinterpretType.Create(lhs, rhsTypeName)));
            IExpression failure = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName));
            return IfBranch.CreateIf(condition, new[] { success }, IfBranch.CreateElse(new[] { failure }));
        }

        public static IExpression Readout(params string[] name)
        {
            return Readout(NameReference.Create(name));
        }
        public static IExpression Readout(IExpression expr)
        {
            return Assignment.CreateStatement(NameReference.Sink(), expr);
        }

        public static IExpression HeapConstructor(string innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(NameReference.Create(innerTypeName),arguments);
        }
        public static IExpression HeapConstructor(NameReference innerTypeName)
        {
            return HeapConstructor(innerTypeName, Enumerable.Empty<FunctionArgument>().ToArray());
            /*
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName));
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, true);
#endif*/
        }
        public static IExpression HeapConstructor(NameReference innerTypeName, params IExpression[] arguments)
        {
            return HeapConstructor(innerTypeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static IExpression HeapConstructor(NameReference innerTypeName, params FunctionArgument[] arguments)
        {
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName), arguments);
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, true, arguments);
#endif
        }

        public static IExpression StackConstructor(NameReference typeName)
        {
            return StackConstructor(typeName, new FunctionArgument[] { });
        }
        public static IExpression StackConstructor(string typeName, params FunctionArgument[] arguments)
        {
            return StackConstructor(NameReference.Create(typeName), arguments);
        }
        public static IExpression StackConstructor(NameReference typeName, params IExpression[] arguments)
        {
            return StackConstructor(typeName, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static IExpression StackConstructor(NameReference typeName, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return constructorCall(typeName, out dummy, false, arguments);
        }
        public static IExpression StackConstructor(NameReference typeName, out NameReference constructorReference,
            params FunctionArgument[] arguments)
        {
            return constructorCall(typeName, out constructorReference, false, arguments);
        }
        private static IExpression constructorCall(NameReference typeName,
            // todo: hack, we don't have nice error translation from generic error to more specific one
            out NameReference constructorReference,
            bool useHeap, params FunctionArgument[] arguments)
        {
            const string local_this = "__this__";
            var var_ref = NameReference.Create(local_this);
            constructorReference = NameReference.Create(var_ref, NameFactory.InitConstructorName);

            var var_decl = VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(typeName, useHeap));
            var init_call = FunctionCall.Constructor(constructorReference, arguments);

            return Block.CreateInitialization(
                // __this__ = alloc()
                var_decl,
                // __this__.init(args)
                init_call,
                // --> __this__
                var_ref );
        }

        public static IExpression Add(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.AddOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression Add(string lhs, string rhs)
        {
            return Add(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression And(IExpression lhs, IExpression rhs)
        {
            return BoolOperator.Create(BoolOperator.OpMode.And, lhs, rhs);
        }
        public static IExpression IsEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.EqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsLess(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.LessOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsGreater(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.GreaterOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression IsNotEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.NotEqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression NotEqual(string lhs, string rhs)
        {
            return IsNotEqual(NameReference.Create(lhs), NameReference.Create(rhs));
        }
        public static IExpression Not(IExpression expr)
        {
            return FunctionCall.Create(NameReference.Create(expr, NameFactory.NotOperator));
        }
        public static IExpression GenericThrow()
        {
            return Throw.Create(HeapConstructor(NameFactory.ExceptionTypeReference()));
        }

        public static IExpression AssertTrue(IExpression condition)
        {
            return IfBranch.CreateIf(ExpressionFactory.Not(condition), new[] { GenericThrow() });
        }

        public static IExpression AssertOptionValue(IExpression option)
        {
            return AssertTrue(optionHasValue(option));
        }

        private static NameReference optionHasValue(IExpression option)
        {
            return NameReference.Create(option, NameFactory.OptionHasValue);
        }

        public static IExpression IfOptionEmpty(IExpression option, params IExpression[] then)
        {
            return IfBranch.CreateIf(ExpressionFactory.Not(optionHasValue(option)), then);
        }

        public static NameReference OptionValue(IExpression option)
        {
            return NameReference.Create(option, NameFactory.OptionValue);
        }
    }
}