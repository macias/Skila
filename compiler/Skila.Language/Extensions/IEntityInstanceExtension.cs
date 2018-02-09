using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityInstanceExtension
    {
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

        public static MutabilityFlag MutabilityOfType(this IEntityInstance @this, ComputationContext ctx)
        {
            // we have to use cache with visited types, because given type A can have a field of type A, 
            // which would lead to infinite checking
            return @this.mutabilityOfType(ctx, new HashSet<IEntityInstance>());
        }

        private static MutabilityFlag mutabilityOfType(this IEntityInstance @this, ComputationContext ctx,
            HashSet<IEntityInstance> visited)
        {
            if (@this.DebugId.Id == 4936)
            {
                ;
            }
            if (!visited.Add(@this))
                return MutabilityFlag.ConstAsSource;

            IEnumerable<MutabilityFlag> mutabilities = @this.EnumerateAll().Select(it => instanceMutability(ctx, it, visited))
                .Where(it => it != MutabilityFlag.ConstAsSource)
                .StoreReadOnly();

            if (mutabilities.Any())
            {
                if (mutabilities.All(it => it == MutabilityFlag.GenericUnknownMutability))
                    return MutabilityFlag.GenericUnknownMutability;
                else
                    return mutabilities.First();
            }

            return MutabilityFlag.ConstAsSource;
        }

        private static MutabilityFlag instanceMutability(ComputationContext ctx, EntityInstance instance, HashSet<IEntityInstance> visited)
        {
            Entities.IEntity target = instance.Target;
            if (!target.IsType()) // namespace
                return MutabilityFlag.ConstAsSource;

            if (ctx.Env.DereferencedOnce(instance, out IEntityInstance val_instance, out bool via_pointer))
            {
                MutabilityFlag mutability = val_instance.mutabilityOfType(ctx, visited);
                return mutability;
            }
            else
            {
                if (instance.OverrideMutability == MutabilityFlag.ForceMutable
                    || instance.OverrideMutability == MutabilityFlag.ForceConst
                    || instance.OverrideMutability == MutabilityFlag.Neutral)
                {
                    return instance.OverrideMutability;
                }

                if (target.Modifier.HasMutable)
                {
                    if (instance.TargetsTemplateParameter)
                        return MutabilityFlag.GenericUnknownMutability;
                    else
                        return MutabilityFlag.ForceMutable;
                }

                return MutabilityFlag.ConstAsSource;
            }
        }
    }

}