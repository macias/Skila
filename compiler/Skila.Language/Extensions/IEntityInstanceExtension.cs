using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class IEntityInstanceExtension
    {
        public static IEntityInstance Rebuild(this IEntityInstance instance, ComputationContext ctx, TypeMutability mutability, bool deep = true)
        {
            return instance.Map(elem =>
            {
                if (deep && ctx.Env.DereferencedOnce(elem, out IEntityInstance val_instance, out bool via_pointer))
                {
                    IEntityInstance val_rebuilt = val_instance.Map(val_elem => val_elem.Rebuild(ctx, mutability, deep));
                    return ctx.Env.Reference(val_rebuilt, mutability, elem.Translation, via_pointer);
                }
                else
                    return elem.Build(mutability);
            });
        }


        public static IEntity Target(this IEntityInstance iinstance)
        {
            if (iinstance is EntityInstance instance)
                return instance.Target;
            else if (iinstance is EntityInstanceUnion union)
                return union.Elements.Single().Target();
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

        public static TypeMutability ComputeMutabilityOfType(this IEntityInstance @this, ComputationContext ctx,
            HashSet<IEntityInstance> visited)
        {
            // we have to use cache with visited types, because given type A can have a field of type A, 
            // which would lead to infinite checking

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

        public static TypeMutability ComputeSurfaceMutabilityOfType(this IEntityInstance @this, ComputationContext ctx)
        {
            // we have to use cache with visited types, because given type A can have a field of type A, 
            // which would lead to infinite checking

            IEnumerable<TypeMutability> mutabilities = @this.EnumerateAll().Select(it => directInstanceMutability(ctx, it))
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
                return TypeMutability.ReadOnly; 

            if (ctx.Env.DereferencedOnce(instance, out IEntityInstance val_instance, out bool via_pointer))
            {
                TypeMutability mutability = val_instance.ComputeMutabilityOfType(ctx, visited);
                return mutability;
            }
            else
            {
                TypeMutability override_mutability = instance.OverrideMutability;
                TypeMutability mask = TypeMutability.None;
                if (instance.OverrideMutability.HasFlag(TypeMutability.Reassignable))
                {
                    mask = TypeMutability.Reassignable;
                    override_mutability ^= TypeMutability.Reassignable;
                }

                switch (override_mutability)
                {
                    case TypeMutability.ForceMutable: return TypeMutability.ForceMutable | mask;
                    case TypeMutability.ForceConst: return TypeMutability.ForceConst | mask;
                    case TypeMutability.DualConstMutable: return TypeMutability.DualConstMutable | mask;
                    case TypeMutability.ReadOnly: return TypeMutability.ReadOnly | mask;
                }


                if (target.Modifier.HasMutable)
                    return TypeMutability.ForceMutable | mask;

                foreach (IEntityInstance arg in instance.TemplateArguments)
                {
                    TypeMutability arg_mutability = arg.MutabilityOfType(ctx);
                    if (arg_mutability != TypeMutability.ForceConst
                        && arg_mutability != TypeMutability.ConstAsSource
                        && arg_mutability != TypeMutability.GenericUnknownMutability)
                        return TypeMutability.ForceMutable | mask;
                }

                const TypeMutability default_mutability = TypeMutability.ConstAsSource;

                if (instance.TargetsTemplateParameter)
                {
                    TypeMutability mutability = TypeMutability.None;
                    if (instance.TemplateParameterTarget.Constraint.Modifier.HasMutable)
                        mutability |= TypeMutability.ForceMutable;
                    if (instance.TemplateParameterTarget.Constraint.Modifier.HasConst)
                        mutability |= TypeMutability.ForceConst;
                    if (instance.TemplateParameterTarget.Constraint.Modifier.HasReassignable)
                        mutability |= TypeMutability.Reassignable;

                    if (mutability != TypeMutability.None)
                    {
                        if (mutability == TypeMutability.Reassignable)
                            mutability |= default_mutability;
                        return mutability | mask;
                    }
                }

                return default_mutability | mask;
            }
        }

        private static TypeMutability directInstanceMutability(ComputationContext ctx, EntityInstance instance)
        {
            Entities.IEntity target = instance.Target;
            if (!target.IsType()) // namespace
                return TypeMutability.ReadOnly; 

            TypeMutability override_mutability = instance.OverrideMutability;
            TypeMutability mask = TypeMutability.None;
            if (instance.OverrideMutability.HasFlag(TypeMutability.Reassignable))
            {
                mask = TypeMutability.Reassignable;
                override_mutability ^= TypeMutability.Reassignable;
            }

            switch (override_mutability)
            {
                case TypeMutability.ForceMutable: return TypeMutability.ForceMutable | mask;
                case TypeMutability.ForceConst: return TypeMutability.ForceConst | mask;
                case TypeMutability.DualConstMutable: return TypeMutability.DualConstMutable | mask;
                case TypeMutability.ReadOnly: return TypeMutability.ReadOnly | mask;
            }


            if (target.Modifier.HasMutable)
                return TypeMutability.ForceMutable | mask;        

            const TypeMutability default_mutability = TypeMutability.ConstAsSource;

            if (instance.TargetsTemplateParameter)
            {
                TypeMutability mutability = TypeMutability.None;
                if (instance.TemplateParameterTarget.Constraint.Modifier.HasMutable)
                    mutability |= TypeMutability.ForceMutable;
                if (instance.TemplateParameterTarget.Constraint.Modifier.HasConst)
                    mutability |= TypeMutability.ForceConst;
                if (instance.TemplateParameterTarget.Constraint.Modifier.HasReassignable)
                    mutability |= TypeMutability.Reassignable;

                if (mutability != TypeMutability.None)
                {
                    if (mutability == TypeMutability.Reassignable)
                        mutability |= default_mutability;
                    return mutability | mask;
                }
            }

            return default_mutability | mask;

        }
    }

}