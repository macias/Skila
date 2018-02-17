﻿using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class TypeMatcher
    {
        private static bool templateMatches(ComputationContext ctx, EntityInstance input,
            EntityInstance target, TypeMatching matching)
        {
            if (input.IsJoker || target.IsJoker)
                return true;

            bool matches = strictTemplateMatches(ctx, input, target, matching);

            TypeDefinition target_type = target.TargetType;

            if (matches || (!(matching.DuckTyping && target_type.IsInterface) && !target_type.IsProtocol))
                return matches;

            VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, input, target, allowPartial: false);

            return vtable != null;
        }

        private static bool strictTemplateMatches(ComputationContext ctx, EntityInstance input, EntityInstance target, TypeMatching matching)
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
                TypeMatch m = input.TemplateArguments[i].TemplateMatchesTarget(ctx,
                    target.TemplateArguments[i],
                    template.Name.Parameters[i].Variance,
                    matching);

                if (m != TypeMatch.Same && m != TypeMatch.Substitute && m != TypeMatch.AutoDereference)
                {
                    return false;
                }
            }

            return true;
        }

        public static TypeMatch Matches(ComputationContext ctx, EntityInstance input, EntityInstance target, TypeMatching matching)
        {
            if (input.DebugId.Id == 12494 && target.DebugId.Id == 12498)
            {
                ;
            }

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

                    TypeMatch m = input.MatchesTarget(ctx, conv_type, matching.WithSlicing(conv_slicing_sub));
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return TypeMatch.InConversion;
                }
            }

            if (ctx.Env.IsReferenceOfType(target))
            {
                if (ctx.Env.IsPointerLikeOfType(input))
                {
                    // note, that we could have reference to pointer in case of the target, we have to unpack both
                    // to the level of the value types
                    int target_dereferences = ctx.Env.Dereference(target, out IEntityInstance inner_target_type);
                    int input_dereferences = ctx.Env.Dereference(input, out IEntityInstance inner_input_type);

                    TypeMatch m = inner_input_type.MatchesTarget(ctx, inner_target_type, matching.WithSlicing(true));
                    if (target_dereferences > input_dereferences && m != TypeMatch.No)
                        m |= TypeMatch.ImplicitReference;
                    return m;
                }
                else
                {
                    ctx.Env.Dereferenced(target, out IEntityInstance inner_target_type);

                    TypeMatch m = input.MatchesTarget(ctx, inner_target_type, matching.WithSlicing(true));
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return m | TypeMatch.ImplicitReference;
                }
            }
            // automatic dereferencing pointers
            else if (ctx.Env.IsPointerLikeOfType(input))
            {
                int input_dereferences;
                {
                    input_dereferences = ctx.Env.Dereference(input, out IEntityInstance dummy);
                }

                // we check if we have more refs/ptrs in input than in target
                if (input_dereferences > (ctx.Env.IsPointerOfType(target) ? 1 : 0))
                {
                    ctx.Env.DereferencedOnce(input, out IEntityInstance inner_input_type, out bool dummy);

                    TypeMatch m = inner_input_type.MatchesTarget(ctx, target, matching.WithSlicing(true));
                    if (m.HasFlag(TypeMatch.Same) || m.HasFlag(TypeMatch.Substitute))
                        return m | TypeMatch.AutoDereference;
                }
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

                    TypeMatch m = conv_type.MatchesTarget(ctx, target, matching.WithSlicing(conv_slicing_sub));
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return TypeMatch.OutConversion;
                }
            }

            if (target.TargetType.AllowSlicedSubstitution)
                matching.AllowSlicing = true;


            if (input.DebugId.Id == 12494 && target.DebugId.Id == 12498)
            {
                ;
            }

            if (matching.AllowSlicing)
            {
                MutabilityFlag input_mutability = input.MutabilityOfType(ctx);

                IEnumerable<TypeAncestor> input_family = new[] { new TypeAncestor(input, 0) }
                    .Concat(input.Inheritance(ctx).OrderedTypeAncestorsIncludingObject
                    // enum substitution works in reverse so we have to exclude these from here
                    .Where(it => !it.AncestorInstance.TargetType.Modifier.HasEnum));
                foreach (TypeAncestor inherited_input in input_family)
                {
                    bool match = templateMatches(ctx, inherited_input.AncestorInstance, target, matching);
                    if (match)
                    {
                        // we cannot shove mutable type in disguise as immutable one, consider such scenario
                        // user could create const wrapper over "*Object" (this is immutable type) and then create its instance
                        // passing some mutable instance, wrapper would be still immutable despite the fact it holds mutable data
                        // this would be disastrous when working concurrently (see more in Documentation/Mutability)

                        MutabilityFlag target_mutability = target.MutabilityOfType(ctx);
                        if (!matching.IgnoreMutability
                            && !MutabilityMatches(input_mutability, target_mutability))
                            return TypeMatch.Mismatched(mutability: true);
                        else if (input == target)
                            return TypeMatch.Same;
                        else
                            return TypeMatch.Substituted(inherited_input.Distance);
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
                    .Concat(target.Inheritance(ctx).OrderedTypeAncestorsIncludingObject)
                    .Where(it => it.AncestorInstance.TargetType.Modifier.HasEnum), matching.WithSlicing(true));

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
                    .Concat(base_of.Select(it => new TypeAncestor(it.Cast<EntityInstance>(), 1))), matching);

                if (m != TypeMatch.No)
                    return m;
            }

            return TypeMatch.No;
        }

        internal static bool MutabilityMatches(MutabilityFlag inputMutability, MutabilityFlag targetMutability)
        {
            switch (inputMutability)
            {
                case MutabilityFlag.DualConstMutable: return true;

                case MutabilityFlag.ForceConst:
                case MutabilityFlag.ConstAsSource:
                    return targetMutability != MutabilityFlag.ForceMutable && targetMutability != MutabilityFlag.GenericUnknownMutability;

                case MutabilityFlag.ForceMutable:
                case MutabilityFlag.GenericUnknownMutability:
                    return targetMutability != MutabilityFlag.ConstAsSource && targetMutability != MutabilityFlag.ForceConst;

                case MutabilityFlag.Neutral: return targetMutability == MutabilityFlag.Neutral;

                default: throw new NotImplementedException();
            }
        }

        public static TypeMatch inversedTypeMatching(ComputationContext ctx, EntityInstance input, IEnumerable<TypeAncestor> targets,
            TypeMatching matching)
        {
            if (!targets.Any())
                return TypeMatch.No;

            EntityInstance target = targets.First().AncestorInstance;

            MutabilityFlag target_mutability = target.MutabilityOfType(ctx);

            foreach (TypeAncestor inherited_target in targets)
            {
                // please note that unlike normal type matching we reversed the types, in enum you can
                // pass base type as descendant!
                bool match = templateMatches(ctx, inherited_target.AncestorInstance, input, matching);

                if (match)
                {
                    MutabilityFlag input_mutability = input.MutabilityOfType(ctx);
                    if (!matching.IgnoreMutability
                        && !MutabilityMatches(target_mutability, input_mutability))
                        return TypeMatch.Mismatched(mutability: true);
                    else if (input == target)
                        return TypeMatch.Same;
                    else
                        return TypeMatch.Substituted(inherited_target.Distance);
                }
            }

            return TypeMatch.No;
        }

        public static bool LowestCommonAncestor(ComputationContext ctx,
            IEntityInstance anyTypeA, IEntityInstance anyTypeB,
            out IEntityInstance result)
        {
            bool a_dereferenced, b_dereferenced;
            bool via_pointer = false;
            {
                a_dereferenced = ctx.Env.DereferencedOnce(anyTypeA, out IEntityInstance deref_a, out bool a_via_pointer);
                b_dereferenced = ctx.Env.DereferencedOnce(anyTypeA, out IEntityInstance deref_b, out bool b_via_pointer);
                if (a_dereferenced && b_dereferenced)
                {
                    anyTypeA = deref_a;
                    anyTypeB = deref_b;
                    via_pointer = a_via_pointer && b_via_pointer;
                }
            }

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

            HashSet<EntityInstance> set_a = type_a.Inheritance(ctx).OrderedAncestorsIncludingObject.Concat(type_a).ToHashSet();
            result = selectFromLowestCommonAncestorPool(ctx, type_b, set_a);
            if (result != null && a_dereferenced && b_dereferenced)
                result = ctx.Env.Reference(result, MutabilityFlag.ConstAsSource, null, via_pointer);
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

            foreach (EntityInstance interface_parent in type.Inheritance(ctx).MinimalParentsIncludingObject
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

            if (closedTemplate.Target.DebugId.Id == 664)
            {
                ;
            }

            return ArgumentsMatchConstraintsOf(ctx, closedTemplate.Target.Name.Parameters, closedTemplate);
        }

        public static ConstraintMatch ArgumentsMatchConstraintsOf(ComputationContext ctx,
            IEnumerable<TemplateParameter> templateParameters, EntityInstance closedTemplate)
        {
            foreach (Tuple<TemplateParameter, IEntityInstance> param_arg in templateParameters
                .SyncZip(closedTemplate.TemplateArguments))
            {
                ConstraintMatch match = param_arg.Item2.ArgumentMatchesParameterConstraints(ctx, closedTemplate, param_arg.Item1);
                if (match != ConstraintMatch.Yes)
                    return match;
            }

            return ConstraintMatch.Yes;
        }

        public static bool InterchangeableTypes(ComputationContext ctx, IEntityInstance eval1, IEntityInstance eval2)
        {
            ctx.Env.Dereference(eval1, out eval1);
            ctx.Env.Dereference(eval2, out eval2);

            // if both types are sealed they are interchangeable only if they are the same
            // if one is not sealed then it has to be possible to subsitute them
            // otherwise they are completely alien and checking is-type/is-same does not make sense
            if (!interchangeableTypesTo(ctx, eval1, eval2))
                return false;
            bool result = interchangeableTypesTo(ctx, eval2, eval1);
            return result;
        }

        private static bool interchangeableTypesTo(ComputationContext ctx, IEntityInstance input, IEntityInstance target)
        {
            bool is_input_sealed = input.EnumerateAll()
                .Select(it => { ctx.Env.Dereference(it, out IEntityInstance result); return result; })
                .SelectMany(it => it.EnumerateAll())
                .All(it => it.Target.Modifier.IsSealed);

            if (is_input_sealed)
            {
                TypeMatch match = input.MatchesTarget(ctx, target, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true).WithIgnoredMutability(true));

                // example: we cannot check if x (of Int) is IAlien because Int is sealed and there is no way
                // it could be something else not available in its inheritance tree
                if (!match.HasFlag(TypeMatch.Same) && !match.HasFlag(TypeMatch.Substitute))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
