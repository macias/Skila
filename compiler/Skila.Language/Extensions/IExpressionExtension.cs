using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

namespace Skila.Language.Extensions
{
    public static class IExpressionExtension
    {
        public static void ValidateValueExpression(this IExpression @this, ComputationContext ctx)
        {
            if (!@this.IsValue() && !@this.Evaluation.Components.IsJoker)
                ctx.AddError(ErrorCode.NoValueExpression, @this);
        }

        public static VariableDeclaration TryGetVariable(this IExpression lhs)
        {
            if (lhs is NameReference name_ref && name_ref.Binding.Match.Target is VariableDeclaration decl)
                return decl;
            else
                return null;
        }

        public static IEntityVariable TryGetEntityVariable(this IExpression lhs)
        {
            if (lhs is NameReference name_ref && name_ref.Binding.Match.Target is IEntityVariable decl)
                return decl;
            else
                return null;
        }
        public static bool IsValue(this IExpression @this)
        {
            IEntity entity = (@this as NameReference)?.Binding.Match.Target;
            return (entity == null || (!entity.IsType() && !entity.IsNamespace()));
        }

        public static bool IsUndef(this IExpression @this)
        {
            return @this is Undef;
        }

        public static bool IsSink(this IExpression @this)
        {
            return @this is NameReference name_ref && name_ref.IsSink;
        }
    }
}
