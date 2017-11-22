using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class TypeMatcher
    {
        private static bool templateMatches(ComputationContext ctx, bool inversedVariance, EntityInstance input,
            EntityInstance target, bool allowSlicing)
        {
            if (input.IsJoker || target.IsJoker)
                return true;

            bool matches = strictTemplateMatches(ctx, inversedVariance, input, target, allowSlicing);

            TypeDefinition target_type = target.TargetType;

            if (matches || (!(ctx.Env.Options.InterfaceDuckTyping && target_type.IsInterface) && !target_type.IsProtocol))
                return matches;

            VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, input, target);

            return vtable != null;
        }

        private static bool strictTemplateMatches(ComputationContext ctx, bool inversedVariance,
            EntityInstance input, EntityInstance target, bool allowSlicing)
        {
            if (input.Target != target.Target)
                return false;

            TemplateDefinition template = target.TargetTemplate;

            for (int i = 0; i < template.Name.Arity; ++i)
            {
                TypeMatch m = input.TemplateArguments[i].TemplateMatchesTarget(ctx, inversedVariance,
                    target.TemplateArguments[i],
                    template.Name.Parameters[i].Variance,
                    allowSlicing);

                if (m != TypeMatch.Pass && m != TypeMatch.AutoDereference)
                {
                    return false;
                }
            }

            return true;
        }

        public static TypeMatch Matches(ComputationContext ctx, bool inversedVariance, EntityInstance input, EntityInstance target,
            bool allowSlicing)
        {
            if (input.IsJoker || target.IsJoker)
                return TypeMatch.Pass;

            if (!target.Target.IsType() || !input.Target.IsType())
                return TypeMatch.No;

            if (input.IsSame(target, jokerMatchesAll: true))
                return TypeMatch.Pass;

            {
                IEnumerable<FunctionDefinition> in_conv = target.TargetType.ImplicitInConverters().StoreReadOnly();
                bool conv_slicing_sub = input.TargetType.AllowSlicedSubstitution;
                foreach (FunctionDefinition func in in_conv)
                {
                    IEntityInstance conv_type = func.Parameters.Single().TypeName.Evaluated(ctx).TranslateThrough(target);
                    if (target == conv_type) // preventing circular conversion check
                        continue;
                    if (input.DebugId.Id == 1978 && target.DebugId.Id == 1996)
                    {
                        ;
                    }

                    if (input.MatchesTarget(ctx, conv_type, conv_slicing_sub) == TypeMatch.Pass)
                        return TypeMatch.InConversion;
                }
            }

            if (ctx.Env.IsReferenceOfType(target))
            {
                if (ctx.Env.IsPointerOfType(input))
                {
                    IEntityInstance inner_target_type = target.TemplateArguments.Single();
                    IEntityInstance inner_input_type = input.TemplateArguments.Single();

                    TypeMatch m = inner_input_type.MatchesTarget(ctx, inner_target_type, true);
                    if (m == TypeMatch.Pass)
                        return m;
                }
                else if (!ctx.Env.IsReferenceOfType(input))
                {
                    IEntityInstance inner_target_type = target.TemplateArguments.Single();

                    if (input.MatchesTarget(ctx, inner_target_type, true) == TypeMatch.Pass)
                        return TypeMatch.ImplicitReference;
                }
            }
            // automatic dereferencing pointers
            else if (ctx.Env.IsPointerOfType(input))
            {
                IEntityInstance inner_input_type = input.TemplateArguments.Single();

                if (inner_input_type.MatchesTarget(ctx, target, true) == TypeMatch.Pass)
                    return TypeMatch.AutoDereference;
            }

            {
                IEnumerable<FunctionDefinition> out_conv = input.TargetType.ImplicitOutConverters().StoreReadOnly();
                bool conv_slicing_sub = input.TargetType.AllowSlicedSubstitution;
                foreach (FunctionDefinition func in out_conv)
                {
                    IEntityInstance conv_type = func.ResultTypeName.Evaluated(ctx).TranslateThrough(input);
                    //if (target == conv_type) // preventing circular conversion check
                    //  continue;
                    if (input.DebugId.Id == 1978 && target.DebugId.Id == 1996)
                    {
                        ;
                    }

                    if (conv_type.MatchesTarget(ctx, target, conv_slicing_sub) == TypeMatch.Pass)
                        return TypeMatch.OutConversion;
                }
            }

            if (target.TargetType.AllowSlicedSubstitution)
                allowSlicing = true;
            else if (!allowSlicing)
            {
                // we already checked if the types are the same
                return TypeMatch.No;
            }

            foreach (EntityInstance inherited_input in new[] { input }.Concat(input.Inheritance(ctx).AncestorsIncludingObject))
            {
                bool match = templateMatches(ctx, inversedVariance, inherited_input, target, allowSlicing);
                if (match)
                {
                    // we cannot shove mutable type in disguise as immutable one, consider such scenario
                    // user could create const wrapper over "*Object" (this is immutable type) and then create its instance
                    // passing some mutable instance, wrapper would be still immutable despite the fact it holds mutable data
                    // this would be disastrous when working concurrently
                    if (!inherited_input.IsImmutableType(ctx) && target.IsImmutableType(ctx))
                        return TypeMatch.No;
                    else
                        return TypeMatch.Pass;
                }
            }

            return TypeMatch.No;
        }

        public static IEntityInstance LowestCommonAncestor(ComputationContext ctx, IEntityInstance anyTypeA, IEntityInstance anyTypeB)
        {
            var type_a = anyTypeA as EntityInstance;
            var type_b = anyTypeB as EntityInstance;
            if (type_a == null || type_b == null)
                return ctx.Env.ObjectType.InstanceOf;

            type_a.Evaluated(ctx);
            type_b.Evaluated(ctx);

            if (type_a.IsJoker)
                return type_b;
            else if (type_b.IsJoker)
                return type_a;

            HashSet<EntityInstance> set_a = type_a.Inheritance(ctx).AncestorsIncludingObject.Concat(type_a).ToHashSet();
            EntityInstance common = selectFromLowestCommonAncestorPool(ctx, type_b, set_a);
            return common ?? ctx.Env.ObjectType.InstanceOf;
        }

        private static EntityInstance selectFromLowestCommonAncestorPool(ComputationContext ctx, EntityInstance type,
            HashSet<EntityInstance> pool)
        {
            if (type == null)
                return null;

            if (pool.Contains(type))
                return type;

            EntityInstance implementation_parent = type.Inheritance(ctx).GetTypeImplementationParent();
            // prefer LCA as implementation
            EntityInstance via_implementation = selectFromLowestCommonAncestorPool(ctx, implementation_parent, pool);

            if (via_implementation != null && via_implementation.IsTypeImplementation)
                return via_implementation;

            foreach (EntityInstance interface_parent in type.Inheritance(ctx).MinimalParentsWithoutObject.Where(it => it != implementation_parent))
            {
                EntityInstance via_interface = selectFromLowestCommonAncestorPool(ctx, interface_parent, pool);
                if (via_interface != null)
                    return via_interface;
            }

            return via_implementation;
        }

        /* public static bool AreOverloadDistinct(EntityInstance type1, EntityInstance type2)
         {            
             return type2 != type1 && !type1.DependsOnTypeParameter && !type2.DependsOnTypeParameter;
         }*/

        public static bool IsStrictAncestorTypeOf(ComputationContext ctx, IEntityInstance ancestor, IEntityInstance descendant)
        {
            return ancestor.IsStrictAncestorOf(ctx, descendant);
        }

        public static ConstraintMatch ArgumentsMatchConstraintsOf(ComputationContext ctx, EntityInstance closedTemplate)
        {
            if (closedTemplate == null || closedTemplate.IsJoker)
                return ConstraintMatch.Yes;

            foreach (Tuple<TemplateParameter, IEntityInstance> param_arg in closedTemplate.Target.Name.Parameters
                .SyncZip(closedTemplate.TemplateArguments))
            {
                ConstraintMatch match = param_arg.Item2.ArgumentMatchesConstraintsOf(ctx, closedTemplate, param_arg.Item1);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }
    }
}
