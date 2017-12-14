using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

namespace Skila.Language.Extensions
{
    public static class IExpressionExtension
    {
        public static void ValidateValueExpression(this IExpression @this, ComputationContext ctx)
        {
            if (!@this.IsValue(ctx.Env.Options) && !@this.Evaluation.Components.IsJoker)
                ctx.AddError(ErrorCode.NoValueExpression, @this);
        }

        public static T TryGetTargetEntity<T>(this IExpression expr,out NameReference nameReference)
            where T : class, IEntity
        {
            nameReference = expr as NameReference;
            if (nameReference!=null)
            {
                return nameReference.Binding.Match.Target as T;
            }
            else
            {
                return null;
            }
        }

        public static bool IsValue(this IExpression @this,IOptions options)
        {
            NameReference nameReference = (@this as NameReference);
            // todo: make it nice, such exception is ugly
            if (nameReference?.Name == NameFactory.BaseVariableName)
                return true;
            IEntity entity = nameReference?.Binding.Match.Target;
            bool result = (entity == null || (!entity.IsType() && !entity.IsNamespace()));
            return result;
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
