using Skila.Language.Semantics;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class INameReferenceExtension
    {
        public static void ValidateTypeNameVariance(this INameReference @this, ComputationContext ctx, VarianceMode typeNamePosition)
        {
            if (!@this.Evaluation.Components.ValidateTypeVariance(ctx, @this, typeNamePosition))
                ctx.AddError(ErrorCode.VarianceForbiddenPosition, @this);
        }


        public static void ValidateHeapTypeName(this INameReference typeName, ComputationContext ctx, INode errorNode = null)
        {
            if (typeName.Evaluation.Components.EnumerateAll()
                .Where(it => !ctx.Env.IsPointerOfType(it) && !ctx.Env.IsReferenceOfType(it))
                .Any(it => it.TargetType.Modifier.HasHeapOnly))
            {
                ctx.AddError(ErrorCode.HeapTypeAsValue, errorNode ?? typeName);
            }
        }

    }
}