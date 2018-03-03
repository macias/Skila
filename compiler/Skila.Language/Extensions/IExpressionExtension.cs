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

        public static T TryGetTargetEntity<T>(this IExpression expr, out NameReference nameReference)
            where T : class, IEntity
        {
            nameReference = expr as NameReference;
            if (nameReference != null)
            {
                return nameReference.Binding.Match.Instance.Target as T;
            }
            else
            {
                return null;
            }
        }

        public static bool TargetsCurrentInstanceMember(this IExpression expr, out IMember member)
        {
            member = expr.TryGetTargetEntity<IMember>(out NameReference name_ref);
            if (member != null && (name_ref.HasThisPrefix || name_ref.HasBasePrefix
                || (name_ref.Prefix == null && member.Owner == expr.EnclosingScope<TypeDefinition>())))
                return true;
            else
                return false;
        }

        public static bool TargetsCurrentTypeMember(this IExpression expr, out IMember member)
        {
            member = expr.TryGetTargetEntity<IMember>(out NameReference name_ref);
            if (member != null)
            {
                if ((name_ref.HasThisPrefix || name_ref.HasBasePrefix
                    || (name_ref.Prefix == null && member.Owner == expr.EnclosingScope<TypeDefinition>())))
                    return true;
                // hitting static member
                else if (member.Modifier.HasStatic && member.Owner == expr.EnclosingScope<TypeDefinition>())
                    return true;
            }

            return false;
        }

        public static bool IsValue(this IExpression @this, IOptions options)
        {
            NameReference nameReference = (@this as NameReference);
            // todo: make it nice, such exception is ugly
            if (nameReference?.Name == NameFactory.BaseVariableName)
                return true;
            IEntity entity = nameReference?.Binding.Match.Instance.Target;
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
