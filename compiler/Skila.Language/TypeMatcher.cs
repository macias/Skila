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

            VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, input, target, allowPartial: false);

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

                if (m != TypeMatch.Same && m != TypeMatch.Substitute && m != TypeMatch.AutoDereference)
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
                return TypeMatch.Same;

            if (!target.Target.IsType() || !input.Target.IsType())
                return TypeMatch.No;

            if (input.IsSame(target, jokerMatchesAll: true))
                return TypeMatch.Same;

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

                    TypeMatch m = input.MatchesTarget(ctx, conv_type, conv_slicing_sub);
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return TypeMatch.InConversion;
                }
            }

            if (ctx.Env.IsReferenceOfType(target))
            {
                if (ctx.Env.IsPointerOfType(input))
                {
                    IEntityInstance inner_target_type = target.TemplateArguments.Single();
                    IEntityInstance inner_input_type = input.TemplateArguments.Single();

                    TypeMatch m = inner_input_type.MatchesTarget(ctx, inner_target_type, allowSlicing: true);
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return m;
                }
                else if (!ctx.Env.IsReferenceOfType(input))
                {
                    IEntityInstance inner_target_type = target.TemplateArguments.Single();

                    TypeMatch m = input.MatchesTarget(ctx, inner_target_type, allowSlicing: true);
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return m | TypeMatch.ImplicitReference;
                }
            }
            // automatic dereferencing pointers
            else if (ctx.Env.IsPointerLikeOfType(input))
            {
                IEntityInstance inner_input_type = input.TemplateArguments.Single();

                TypeMatch m = inner_input_type.MatchesTarget(ctx, target, allowSlicing: true);
                if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                    return m | TypeMatch.AutoDereference;
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

                    TypeMatch m = conv_type.MatchesTarget(ctx, target, conv_slicing_sub);
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return TypeMatch.OutConversion;
                }
            }

            if (target.TargetType.AllowSlicedSubstitution)
                allowSlicing = true;


            if (allowSlicing)
            {
                foreach (EntityInstance inherited_input in new[] { input }.Concat(input.Inheritance(ctx).AncestorsIncludingObject
                    // enum substitution works in reverse so we have to exclude these from here
                    .Where(it => !it.TargetType.Modifier.HasEnum)))
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
                        else if (input == target)
                            return TypeMatch.Same;
                        else
                            return TypeMatch.Substitute;
                    }
                }
            }

            if (input.TargetType.Modifier.HasEnum)
                foreach (EntityInstance inherited_target in new[] { target }.Concat(target.Inheritance(ctx).AncestorsIncludingObject)
                    .Where(it => it.TargetType.Modifier.HasEnum))
                {
                    // please note that unlike normal type matching we reversed the types, in enum you can
                    // pass base type as descendant!
                    bool match = templateMatches(ctx, inversedVariance, inherited_target, input,
                        // since we compare only enums here we allow slicing (because it is not slicing, just passing single int)
                        allowSlicing: true);

                    if (match)
                    {
                        // we cannot shove mutable type in disguise as immutable one, consider such scenario
                        // user could create const wrapper over "*Object" (this is immutable type) and then create its instance
                        // passing some mutable instance, wrapper would be still immutable despite the fact it holds mutable data
                        // this would be disastrous when working concurrently
                        if (!inherited_target.IsImmutableType(ctx) && input.IsImmutableType(ctx))
                            return TypeMatch.No;
                        else if (input == target)
                            return TypeMatch.Same;
                        else
                            return TypeMatch.Substitute;
                    }
                }

            return TypeMatch.No;
        }

        public static bool LowestCommonAncestor(ComputationContext ctx,
            IEntityInstance anyTypeA, IEntityInstance anyTypeB,
            out IEntityInstance result)
        {
            var type_a = anyTypeA as EntityInstance;
            var type_b = anyTypeB as EntityInstance;
            if (type_a == null)
            {
                result = type_b;
                return type_b != null;
            }
            else if (type_b == null)
            {
                result = type_a;
                return type_a != null;
            }

            type_a.Evaluated(ctx);
            type_b.Evaluated(ctx);

            if (type_a.IsJoker)
            {
                result = type_b;
                return true;
            }
            else if (type_b.IsJoker)
            {
                result = type_a;
                return true;
            }

            HashSet<EntityInstance> set_a = type_a.Inheritance(ctx).AncestorsIncludingObject.Concat(type_a).ToHashSet();
            result = selectFromLowestCommonAncestorPool(ctx, type_b, set_a);
            return result != null;
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

            if (via_implementation != null)
                return via_implementation;

            foreach (EntityInstance interface_parent in type.Inheritance(ctx).MinimalParentsWithObject
                .Where(it => it != implementation_parent))
            {
                EntityInstance via_interface = selectFromLowestCommonAncestorPool(ctx, interface_parent, pool);
                if (via_interface != null)
                    return via_interface;
            }

            return null;
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
