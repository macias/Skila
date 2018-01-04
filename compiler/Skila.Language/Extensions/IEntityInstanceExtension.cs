using Skila.Language.Entities;
using System;
using System.Collections.Generic;

namespace Skila.Language.Extensions
{
    public static class IEntityInstanceExtension
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

        private static bool IsImmutableType(this IEntityInstance @this, ComputationContext ctx, HashSet<IEntityInstance> visited)
        {
            if (!visited.Add(@this))
                return true;

            foreach (EntityInstance instance in @this.Enumerate())
            {
                Entities.IEntity target = instance.Target;
                if (!target.IsType())
                    throw new Exception("Internal error");

                if (target.Modifier.HasMutable || instance.OverrideMutability)
                    return false;

                foreach (VariableDeclaration field in target.CastType().AllNestedFields)
                {
                    IEntityInstance eval = field.Evaluated(ctx);
                    if (!eval.IsImmutableType(ctx, visited))
                        return false;
                }

                if (ctx.Env.Dereferenced(instance, out IEntityInstance val_instance, out bool via_pointer))
                {
                    if (!val_instance.IsImmutableType(ctx, visited))
                        return false;
                }
            }

            return true;
        }
    }
}