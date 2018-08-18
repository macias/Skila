using Skila.Language.Entities;
using Skila.Language.Semantics;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityExtension
    {
        /// <returns>null for non-methods and alike</returns>
        public static TypeDefinition ContainingType(this IEntity @this)
        {
            TemplateDefinition scope = @this.EnclosingScope<TemplateDefinition>();
            return scope.IsType() ? scope.CastType() : null;
        }

        public static void ValidateReferenceAssociatedReference(this IEntity entity,ComputationContext ctx)
        {
            IEntityInstance eval = entity.Evaluation.Components;
            if (ctx.Env.IsReferenceOfType(eval))
                return;

            ctx.Env.Dereferenced(eval,out eval);

            if (eval.EnumerateAll().Any(it => it.TargetType.Modifier.HasAssociatedReference))
                ctx.AddError(ErrorCode.AssociatedReferenceRequiresPassingByReference, entity);
        }

        public static bool IsPropertyOwned(this IEntity @this)
        {
            return @this.Owner is Property;
        }

        public static bool IsTypeContained(this IEntityVariable @this)
        {
            return @this.ContainingType() != null;
        }

        public static bool IsFunctionContained(this IEntityVariable @this)
        {
            TemplateDefinition scope = @this.EnclosingScope<TemplateDefinition>();
            return scope.IsFunction();
        }

    }
}
