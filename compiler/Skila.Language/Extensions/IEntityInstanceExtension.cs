using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityInstanceExtension
    {
        public static IEntityInstance Rebuild(this IEntityInstance instance, ComputationContext ctx, MutabilityOverride mutability)
        {
            return instance.Map(elem =>
            {
                if (ctx.Env.DereferencedOnce(elem, out IEntityInstance val_instance, out bool via_pointer))
                {
                    IEntityInstance val_rebuilt = val_instance.Map(val_elem => val_elem.Rebuild(ctx, mutability));
                    return ctx.Env.Reference(val_rebuilt, mutability, elem.Translation, via_pointer);
                }
                else
                    return elem.Build(mutability);
            });
        }


        public static IEntity Target(this IEntityInstance instance)
        {
            if (instance is EntityInstance)
                return (instance as EntityInstance).Target;
            else if (instance is EntityInstanceUnion)
                return (instance as EntityInstanceUnion).Instances.Single().Target();
            else
                throw new NotImplementedException();
        }
        public static T TranslateThrough<T>(this T @this, IEntityInstance closedTemplate)
            where T : IEntityInstance
        {
            if (closedTemplate == null)
                return @this;

            bool translated = false;
            return closedTemplate.TranslationOf(@this, ref translated, closedTranslation: null).Cast<T>();
        }

        public static T TranslateThrough<T>(this T @this, IEnumerable<IEntityInstance> closedTemplates)
            where T : IEntityInstance
        {
            foreach (IEntityInstance closed in closedTemplates)
                @this = @this.TranslateThrough(closed);
            return @this;
        }

        public static TypeMutability MutabilityOfType(this IEntityInstance @this, ComputationContext ctx)
        {
            // we have to use cache with visited types, because given type A can have a field of type A, 
            // which would lead to infinite checking
            return @this.mutabilityOfType(ctx, new HashSet<IEntityInstance>());
        }

        private static TypeMutability mutabilityOfType(this IEntityInstance @this, ComputationContext ctx,
            HashSet<IEntityInstance> visited)
        {
            if (@this.DebugId.Id == 4936)
            {
                ;
            }
            if (!visited.Add(@this))
                return TypeMutability.ConstAsSource;

            IEnumerable<TypeMutability> mutabilities = @this.EnumerateAll().Select(it => instanceMutability(ctx, it, visited))
                .Where(it => it != TypeMutability.ConstAsSource)
                .StoreReadOnly();

            if (mutabilities.Any())
            {
                if (mutabilities.All(it => it == TypeMutability.GenericUnknownMutability))
                    return TypeMutability.GenericUnknownMutability;
                else
                    return mutabilities.First();
            }

            return TypeMutability.ConstAsSource;
        }

        private static TypeMutability instanceMutability(ComputationContext ctx, EntityInstance instance, HashSet<IEntityInstance> visited)
        {
            Entities.IEntity target = instance.Target;
            if (!target.IsType()) // namespace
                return TypeMutability.Neutral;

            if (ctx.Env.DereferencedOnce(instance, out IEntityInstance val_instance, out bool via_pointer))
            {
                TypeMutability mutability = val_instance.mutabilityOfType(ctx, visited);
                return mutability;
            }
            else
            {
                switch (instance.OverrideMutability)
                {
                    case MutabilityOverride.ForceMutable: return TypeMutability.Mutable;
                    case MutabilityOverride.ForceConst: return TypeMutability.Const;
                    case MutabilityOverride.DualConstMutable: return TypeMutability.DualConstMutable;
                    case MutabilityOverride.Neutral: return TypeMutability.Neutral;
                }

                if (target.Modifier.HasMutable)
                {
                    if (instance.TargetsTemplateParameter)
                        return TypeMutability.GenericUnknownMutability;
                    else
                        return TypeMutability.Mutable;
                }

                return TypeMutability.ConstAsSource;
            }
        }
    }

}