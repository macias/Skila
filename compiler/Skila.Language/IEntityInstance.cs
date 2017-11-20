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
        public static T TranslateThrough<T>(this T @this, IEnumerable<IEntityInstance> closedTemplates)
            where T : IEntityInstance
        {
            foreach (IEntityInstance closed in closedTemplates)
                @this = @this.TranslateThrough(closed);
            return @this;
        }

        public static bool IsImmutableType(this IEntityInstance @this, ComputationContext ctx)
        {
            // we have to use cache with visited types, because given type A can have a field of type A, 
            // which would lead to infinite checking
            return @this.IsImmutableType(ctx, new HashSet<IEntityInstance>());
        }

        private static bool IsImmutableType(this IEntityInstance @this, ComputationContext ctx,HashSet<IEntityInstance> visited)
        {
            if (!visited.Add(@this))
                return true;

            foreach (EntityInstance instance in @this.Enumerate())
            {
                Entities.IEntity target = instance.Target;
                if (!target.IsType())
                    throw new Exception("Internal error");

                if (!target.Modifier.HasImmutable)
                    return false;

                foreach (VariableDeclaration field in target.CastType().AllNestedFields)
                {
                    IEntityInstance eval = field.Evaluated(ctx);
                    if (!eval.IsImmutableType(ctx,visited))
                        return false;
                }

                if ((ctx.Env.IsPointerLikeOfType(instance)) && !instance.TemplateArguments.Single().IsImmutableType(ctx,visited))
                    return false;
            }

            return true;
        }
    }
}