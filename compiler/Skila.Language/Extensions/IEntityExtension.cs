using Skila.Language.Entities;
using Skila.Language.Semantics;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityExtension
    {
        public static void ValidateReferenceAssociatedReference(this IEntity entity,ComputationContext ctx)
        {
            IEntityInstance eval = entity.Evaluation.Components;
            if (ctx.Env.IsReferenceOfType(eval))
                return;

            ctx.Env.Dereferenced(eval,out eval);

            if (eval.EnumerateAll().Any(it => it.TargetType.Modifier.HasAssociatedReference))
                ctx.AddError(ErrorCode.AssociatedReferenceRequiresPassingByReference, entity);
        }
    }
}
