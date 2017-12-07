using Skila.Language.Entities;
using Skila.Language.Flow;
using System.Linq;

namespace Skila.Language.Expressions
{
    public static class ExpressionFactory
    {
        public static FunctionCall BaseInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.Create(NameFactory.BaseVariableName, NameFactory.InitConstructorName), 
                arguments);
        }
        public static FunctionCall ThisInit(params FunctionArgument[] arguments)
        {
            return FunctionCall.Constructor(NameReference.Create(NameFactory.ThisVariableName, NameFactory.InitConstructorName),
                arguments);
        }
        public static IExpression Cast(IExpression lhs, INameReference rhsTypeName)
        {
            IExpression condition = IsType.Create(lhs, rhsTypeName);
            IExpression success = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName),
                FunctionArgument.Create(ReinterpretType.Create(lhs, rhsTypeName)));
            IExpression failure = ExpressionFactory.StackConstructor(NameFactory.OptionTypeReference(rhsTypeName));
            return IfBranch.CreateIf(condition, new[] { success }, IfBranch.CreateElse(new[] { failure }));
        }

        public static IExpression Readout(string name)
        {
            return Readout(NameReference.Create(name));
        }
        public static IExpression Readout(IExpression expr)
        {
            return Assignment.CreateStatement(NameReference.Sink(), expr);
        }

        public static IExpression HeapConstructor(string innerTypeName)
        {
            return HeapConstructor(NameReference.Create(innerTypeName));
        }
        public static IExpression HeapConstructor(NameReference innerTypeName)
        {
#if USE_NEW_CONS
            return FunctionCall.Create(NameReference.Create(innerTypeName, NameFactory.NewConstructorName));
#else
            NameReference dummy;
            return constructorCall(innerTypeName, out dummy, true);
#endif
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

        public static IExpression StackConstructor(string typeName, params FunctionArgument[] arguments)
        {
            return StackConstructor(NameReference.Create(typeName), arguments);
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
            var var_decl = VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(typeName, useHeap));
            var var_ref = NameReference.Create(local_this);
            constructorReference = NameReference.Create(var_ref, NameFactory.InitConstructorName);
            var init_call = FunctionCall.Constructor(constructorReference, arguments);

            return Block.CreateExpression(new IExpression[] { var_decl, init_call, var_ref });
        }

        public static IExpression Add(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.AddOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression And(IExpression lhs, IExpression rhs)
        {
            return BoolOperator.Create(BoolOperator.OpMode.And, lhs, rhs);
        }
        public static IExpression Equal(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.EqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression NotEqual(IExpression lhs, IExpression rhs)
        {
            return FunctionCall.Create(NameReference.Create(lhs, NameFactory.NotEqualOperator), FunctionArgument.Create(rhs));
        }
        public static IExpression NotEqual(string lhs, string rhs)
        {
            return NotEqual(NameReference.Create(lhs), NameReference.Create(rhs));
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