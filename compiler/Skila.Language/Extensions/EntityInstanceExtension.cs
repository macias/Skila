using Skila.Language.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Extensions
{
    public static class EntityInstanceExtension
    {
        public static IEnumerable<TypeDefinition> AvailableTraits(this EntityInstance instance,ComputationContext ctx)
        {
            IEntityScope scope = instance.Target.Cast<TemplateDefinition>();

            if (scope is TypeDefinition typedef)
            {
                foreach (TypeDefinition trait in typedef.AssociatedTraits)
                {
                    // todo: once computed which traits fit maybe we could cache them within given instance?
                    ConstraintMatch match = TypeMatcher.ArgumentsMatchConstraintsOf(ctx, trait.Name.Parameters, instance);
                    if (match != ConstraintMatch.Yes)
                        continue;

                    yield return trait;
                }
            }

        }

        public static IEnumerable<EntityInstance> PrimaryAncestors(this EntityInstance instance, ComputationContext ctx)
        {
            EntityInstance primary_parent = instance.Inheritance(ctx).MinimalParentsIncludingObject.FirstOrDefault();
            if (primary_parent == null)
                return Enumerable.Empty<EntityInstance>();
            else
                return new[] { primary_parent }.Concat(primary_parent.PrimaryAncestors(ctx));
        }

        public static bool IsOfType(this EntityInstance instance, TypeDefinition target)
        {
            return /*instance.IsJoker ||*/ (instance.Target.IsType() && target == instance.Target);
        }

        public static VirtualTable BuildDuckVirtualTable(ComputationContext ctx, EntityInstance input, EntityInstance target,
            // this is used when building for types intersection, no single type can cover entire aggregated type
            // so we build only partial virtual table for each element of type intersection
            // for example consider "*Double & *String" -- such super type would have "sqrt" function and "substr" as well
            // but bulting regular vtable Double->DoubleString would be impossible, because (for example) String does not have "sqrt"
            bool allowPartial)
        {
            VirtualTable vtable;
            if (!input.TryGetDuckVirtualTable(target, out vtable))
            {
                Dictionary<FunctionDefinition, FunctionDefinition> mapping
                    = TypeDefinitionExtension.PairDerivations(ctx, target, input.TargetType.NestedFunctions)
                    .Where(it => it.Derived != null)
                    .ToDictionary(it => it.Base, it => it.Derived);

                bool is_partial = false;

                foreach (FunctionDefinition base_func in target.TargetType.NestedFunctions)
                    if (base_func.Modifier.HasAbstract && !mapping.ContainsKey(base_func))
                    {
                        if (allowPartial)
                            is_partial = true;
                        else
                        {
                            mapping = null;
                            break;
                        }
                    }

                if (mapping != null)
                {
                    vtable = new VirtualTable(mapping, is_partial);
                    input.AddDuckVirtualTable(target, vtable);
                }
            }

            return vtable;
        }


    }
}
