using Skila.Language.Entities;
using Skila.Language.Flow;

namespace Skila.Language.Expressions
{
    public static class ExpressionFactory
    {
        public static IExpression HeapConstructorCall(NameReference innerTypeName, params FunctionArgument[] arguments)
        {
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName), arguments);
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, true, arguments);
#endif
        }

        public static IExpression StackConstructorCall(NameReference typeName, params FunctionArgument[] arguments)
        {
            NameReference dummy;
            return constructorCall(typeName, out dummy, false, arguments);
        }
        public static IExpression StackConstructorCall(NameReference typeName, out NameReference constructorReference,
            params FunctionArgument[] arguments)
        {
            return constructorCall(typeName, out constructorReference, false, arguments);
        }
        private static IExpression constructorCall(NameReference typeName,
            // todo: hack, we don't have nice error translation from generic error to more specific one
            out NameReference constructorReference,
            bool useHeap, FunctionArgument[] arguments)
        {
            const string local_this = "__this__";
            var var_decl = VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(typeName, useHeap));
            var var_ref = NameReference.Create(local_this);
            constructorReference = NameReference.Create(var_ref, NameFactory.InitConstructorName);
            var init_call = FunctionCall.Create(constructorReference, arguments);

            return Block.CreateExpression(new IExpression[] { var_decl, init_call, var_ref });
        }

        public static IExpression AddOperator(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.AddOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression NotOperator(IExpression expr)
        {
            return FunctionCall.Create(NameReference.Create(expr, NameFactory.NotOperator));
        }
        public static IExpression GenericThrow()
        {
            return Throw.Create(HeapConstructorCall(NameFactory.ExceptionTypeReference()));
        }

        public static IExpression AssertTrue(IExpression condition)
        {
            return IfBranch.CreateIf(ExpressionFactory.NotOperator(condition), new[] { GenericThrow() });
        }

        public static IExpression AssertOptionValue(IExpression option)
        {
            return AssertTrue(NameReference.Create(option,NameFactory.OptionHasValue));
        }
    }
}