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
                if (input.DebugId.Id == 108975)
                {
                    ;
                }
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
                bool input_immutable = input.IsImmutableType(ctx);

                if (input.DebugId.Id == 108967 && target.DebugId.Id == 108939)
                {
                    ;
                }
                foreach (TypeAncestor inherited_input in new[] { new TypeAncestor(input, 0) }
                    .Concat(input.Inheritance(ctx).TypeAncestorsIncludingObject
                    // enum substitution works in reverse so we have to exclude these from here
                    .Where(it => !it.AncestorInstance.TargetType.Modifier.HasEnum)))
                {
                    bool match = templateMatches(ctx, inversedVariance, inherited_input.AncestorInstance, target, allowSlicing);
                    if (match)
                    {
                        // we cannot shove mutable type in disguise as immutable one, consider such scenario
                        // user could create const wrapper over "*Object" (this is immutable type) and then create its instance
                        // passing some mutable instance, wrapper would be still immutable despite the fact it holds mutable data
                        // this would be disastrous when working concurrently (see more in Documentation/Mutability)

                        bool target_immutable = target.IsImmutableType(ctx);
                        if (input_immutable != target_immutable)
                            return TypeMatch.No;
                        else if (input == target)
                            return TypeMatch.Same;
                        else
                            return TypeMatch.Substitution(inherited_input.Distance);
                    }
                }
            }

            if (input.TargetType.Modifier.HasEnum)
            {
                // another option for enum-"inheritance" would be dropping it altogether, and copying all the values from the
                // base enum. Adding conversion constructor from base to child type will suffice and allow to get rid
                // of those enum-inheritance matching

                // since we compare only enums here we allow slicing (because it is not slicing, just passing single int)
                TypeMatch m = inversedTypeMatching(ctx, input, new[] { new TypeAncestor(target, 0) }
                    .Concat(target.Inheritance(ctx).TypeAncestorsIncludingObject)
                    .Where(it => it.AncestorInstance.TargetType.Modifier.HasEnum), inversedVariance, allowSlicing: true);

                if (m != TypeMatch.No)
                    return m;
            }

            if (target.TargetsTemplateParameter)
            {
                // template parameter can have reversed inheritance defined via base-of constraints

                // todo: at this it should be already evaluated, so constraints should be surfables
                IEnumerable<IEntityInstance> base_of 
                    = target.TemplateParameterTarget.Constraint.BaseOfNames.Select(it => it.Evaluated(ctx));

                TypeMatch m = inversedTypeMatching(ctx, input, new[] { new TypeAncestor(target, 0) }
                    .Concat(base_of.Select(it => new TypeAncestor(it.Cast<EntityInstance>(), 1))), inversedVariance, allowSlicing);

                if (m != TypeMatch.No)
                    return m;
            }

            return TypeMatch.No;
        }

        public static TypeMatch inversedTypeMatching(ComputationContext ctx, EntityInstance input, IEnumerable<TypeAncestor> targets,
            bool inversedVariance, bool allowSlicing)
        {
            if (!targets.Any())
                return TypeMatch.No;

            EntityInstance target = targets.First().AncestorInstance;

            bool target_immutable = target.IsImmutableType(ctx);

            foreach (TypeAncestor inherited_target in targets)
            {
                // please note that unlike normal type matching we reversed the types, in enum you can
                // pass base type as descendant!
                bool match = templateMatches(ctx, inversedVariance, inherited_target.AncestorInstance, input,
                    allowSlicing);

                if (match)
                {
                    bool input_immutable = input.IsImmutableType(ctx);
                    if (input_immutable != target_immutable)
                        return TypeMatch.No;
                    else if (input == target)
                        return TypeMatch.Same;
                    else
                        return TypeMatch.Substitution(inherited_target.Distance);
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

            if (closedTemplate.Target.DebugId.Id == 3703)
            {
                ;
            }

            foreach (Tuple<TemplateParameter, IEntityInstance> param_arg in closedTemplate.Target.Name.Parameters
                .SyncZip(closedTemplate.TemplateArguments))
            {
                ConstraintMatch match = param_arg.Item2.ArgumentMatchesParameterConstraints(ctx, closedTemplate, param_arg.Item1);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }
    }
}
