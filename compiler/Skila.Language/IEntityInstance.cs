using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public interface IEntityInstance
    {
        INameReference NameOf { get; }
        bool DependsOnTypeParameter_UNUSED { get; }

        IEnumerable<EntityInstance> Enumerate();
        bool IsJoker { get; }

        bool IsValueType(ComputationContext ctx);

        IEntityInstance TranslateThrough(EntityInstance closedTemplate, ref bool translated);
        IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated);
        ConstraintMatch ArgumentMatchesConstraintsOf(ComputationContext ctx, EntityInstance verifiedInstance, TemplateParameter param);

        TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target, VarianceMode variance, bool allowSlicing);
        TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input, VarianceMode variance, bool allowSlicing);

        // are types can be assigned or passed
        TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing);
        TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing);

        bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor);
        bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant);

        bool IsSame(IEntityInstance other, bool jokerMatchesAll);
        // checks if types are distinct from each other for function overloading validation
        bool IsOverloadDistinctFrom(IEntityInstance other);
    }

    public static class IEntityInstanceExtensions
    {
        public static T TranslateThrough<T>(this T @this, IEntityInstance closedTemplate)
            where T : IEntityInstance
        {
            if (closedTemplate == null)
                return @this;

            bool translated = false;
            return closedTemplate.TranslationOf(@this, ref translated).Cast<T>();
        }

        public static bool IsImmutableType(this IEntityInstance @this, ComputationContext ctx)
        {
            foreach (EntityInstance instance in @this.Enumerate())
            {
                Entities.IEntity target = instance.Target;
                if (!target.IsType())
                    throw new Exception("Internal error");

                if (!target.Modifier.HasConst)
                    return false;

                foreach (VariableDeclaration field in target.CastType().AllNestedFields)
                {
                    if (!field.Evaluated(ctx).IsImmutableType(ctx))
                        return false;
                }

                if ((ctx.Env.IsPointerOfType(instance) || ctx.Env.IsReferenceOfType(instance)) 
                    && !instance.TemplateArguments.Single().IsImmutableType(ctx))
                    return false;
            }

            return true;
        }
    }
}