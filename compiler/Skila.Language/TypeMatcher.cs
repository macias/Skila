using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class TypeMatcher
    {
        private static bool templateMatches(ComputationContext ctx, EntityInstance input,
            EntityInstance target, TypeMatching matching,
            out TypeMatch failMatch)
        {
            if (input.IsJoker || target.IsJoker)
            {
                failMatch = TypeMatch.Same;
                return true;
            }

            bool matches = strictTemplateMatches(ctx, input, target, matching, out failMatch);

            TypeDefinition target_type = target.TargetType;

            if (matches || (!(matching.DuckTyping && target_type.IsInterface) && !target_type.IsProtocol))
                return matches;

            VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, input, target, allowPartial: false);

            if (vtable == null)
                return false;
            else
            {
                failMatch = TypeMatch.Same;
                return true;
            }
        }

        private static bool strictTemplateMatches(ComputationContext ctx, EntityInstance input, EntityInstance target,
            TypeMatching matching,
            out TypeMatch failMatch)
        {
            if (input.Target != target.Target)
            {
                failMatch = TypeMatch.No;
                return false;
            }

            TemplateDefinition template = target.TargetTemplate;

            for (int i = 0; i < template.Name.Arity; ++i)
            {
                failMatch = input.TemplateArguments[i].TemplateMatchesTarget(ctx,
                    target.TemplateArguments[i],
                    template.Name.Parameters[i].Variance,
                    matching);

                if (!failMatch.Passed)
                    return false;
            }

            failMatch = TypeMatch.Same;
            return true;
        }

        public static TypeMatch Matches(ComputationContext ctx, EntityInstance input, EntityInstance target, TypeMatching matching)
        {
            if (input.IsJoker || target.IsJoker)
                return TypeMatch.Same;

            if (!target.Target.IsType() || !input.Target.IsType())
                return TypeMatch.No;

            if (input.Lifetime.IsAttached)
                matching = matching.WithLifetimeCheck(true, input.Lifetime, target.Lifetime);


            {
                IEnumerable<FunctionDefinition> in_conv = target.TargetType.ImplicitInConverters().StoreReadOnly();
                bool conv_slicing_sub = input.TargetType.AllowSlicedSubstitution;
                foreach (FunctionDefinition func in in_conv)
                {
                    IEntityInstance conv_type = func.Parameters.Single().TypeName.Evaluated(ctx, EvaluationCall.AdHocCrossJump).TranslateThrough(target);
                    if (target.IsIdentical(conv_type)) // preventing circular conversion check
                        continue;

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

                    TypeMatch m = inner_input_type.MatchesTarget(ctx, inner_target_type, matching
                        .WithSlicing(true)
                        .WithMutabilityCheckRequest(true)
                        .WithLifetimeCheck(true, ctx.Env.IsReferenceOfType(input) ? input.Lifetime : Lifetime.Timeless, target.Lifetime));
                    if (target_dereferences > input_dereferences && !m.IsMismatch())
                        m |= TypeMatch.ImplicitReference;
                    return m;
                }
                else
                {
                    ctx.Env.Dereferenced(target, out IEntityInstance inner_target_type);

                    TypeMatch m = input.MatchesTarget(ctx, inner_target_type, matching
                        .WithSlicing(true)
                        .WithMutabilityCheckRequest(true)
                        .WithLifetimeCheck(true, input.Lifetime, target.Lifetime));
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return m | TypeMatch.ImplicitReference;
                    else
                        return m;
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
                    if (m.Passed)
                        return m | TypeMatch.AutoDereference;
                }
                else
                    matching = matching.WithMutabilityCheckRequest(true)
                        ;
            }

            if (ctx.Env.IsReferenceOfType(input) && ctx.Env.IsPointerOfType(target))
            {
                // note, that we could have reference to pointer in case of the target, we have to unpack both
                // to the level of the value types
                ctx.Env.DereferencedOnce(target, out IEntityInstance inner_target_type, out bool dummy1);
                ctx.Env.DereferencedOnce(input, out IEntityInstance inner_input_type, out bool dummy2);

                TypeMatch m = inner_input_type.MatchesTarget(ctx, inner_target_type, matching
                    .WithSlicing(true)
                    .WithMutabilityCheckRequest(true)
                    .WithLifetimeCheck(true, input.Lifetime, Lifetime.Timeless));
                if (!m.IsMismatch())
                    m |= TypeMatch.Attachment;
                return m;
            }


            {
                IEnumerable<FunctionDefinition> out_conv = input.TargetType.ImplicitOutConverters().StoreReadOnly();
                bool conv_slicing_sub = input.TargetType.AllowSlicedSubstitution;
                foreach (FunctionDefinition func in out_conv)
                {
                    IEntityInstance conv_type = func.ResultTypeName.Evaluated(ctx, EvaluationCall.AdHocCrossJump).TranslateThrough(input);
                    //if (target == conv_type) // preventing circular conversion check
                    //  continue;

                    TypeMatch m = conv_type.MatchesTarget(ctx, target, matching.WithSlicing(conv_slicing_sub));
                    if (m == TypeMatch.Same || m == TypeMatch.Substitute)
                        return TypeMatch.OutConversion;
                }
            }

            if (target.TargetType.AllowSlicedSubstitution)
                matching.AllowSlicing = true;


            TypeMatch match = TypeMatch.No;

            TypeMutability input_mutability = input.MutabilityOfType(ctx);
            if (matching.AllowSlicing)
            {
                IEnumerable<TypeAncestor> input_family = new[] { new TypeAncestor(input, 0) }
                    .Concat(input.Inheritance(ctx).OrderedTypeAncestorsIncludingObject
                    // enum substitution works in reverse so we have to exclude these from here
                    .Where(it => !it.AncestorInstance.TargetType.Modifier.HasEnum));

                foreach (TypeAncestor inherited_input in input_family)
                {
                    if (matchTypes(ctx, input_mutability, input.Lifetime, inherited_input.AncestorInstance,
                        target, matching, inherited_input.Distance, out TypeMatch m))
                        return m;
                    else if (m != TypeMatch.No) // getting more specific rejection than just bare one
                        match = m;
                }
            }
            // when slicing is disabled we can compare try to substitute only the same type
            else if (input.Target == target.Target)
            {
                if (matchTypes(ctx, input_mutability, input.Lifetime, input, target, matching, distance: 0, match: out match))
                {
                    if (match != TypeMatch.Same && !match.IsMismatch())
                        throw new Exception($"Internal exception {match}");
                    return match;
                }
            }

            if (input.TargetType.Modifier.HasEnum)
            {
                // another option for enum-"inheritance" would be dropping it altogether, and copying all the values from the
                // base enum. Adding conversion constructor from base to child type will suffice and allow to get rid
                // of those enum-inheritance matching

                // since we compare only enums here we allow slicing (because it is not slicing, just passing single int)
                match = inversedTypeMatching(ctx, input, new[] { new TypeAncestor(target, 0) }
                    .Concat(target.Inheritance(ctx).OrderedTypeAncestorsIncludingObject)
                    .Where(it => it.AncestorInstance.TargetType.Modifier.HasEnum), matching.WithSlicing(true));

                if (!match.IsMismatch())
                    return match;
            }

            if (target.TargetsTemplateParameter)
            {
                // template parameter can have reversed inheritance defined via base-of constraints
                // todo: at this it should be already evaluated, so constraints should be surfables
                IEnumerable<IEntityInstance> base_of
                    = target.TemplateParameterTarget.Constraint.BaseOfNames.Select(it => it.Evaluated(ctx, EvaluationCall.AdHocCrossJump));

                match = inversedTypeMatching(ctx, input, new[] { new TypeAncestor(target, 0) }
                    .Concat(base_of.Select(it => new TypeAncestor(it.Cast<EntityInstance>(), 1))), matching);

                if (!match.IsMismatch())
                    return match;
            }

            return match;
        }

        private static bool matchTypes(ComputationContext ctx, TypeMutability inputMutability,
            Lifetime inputLifetime,
            EntityInstance input, EntityInstance target,
            TypeMatching matching, int distance, out TypeMatch match)
        {
            bool is_matched = templateMatches(ctx, input, target, matching, out TypeMatch fail_match);
            if (!is_matched)
            {
                match = fail_match;
                return false;
            }

            // we cannot shove mutable type in disguise as immutable one, consider such scenario
            // user could create const wrapper over "*Object" (this is immutable type) and then create its instance
            // passing some mutable instance, wrapper would be still immutable despite the fact it holds mutable data
            // this would be disastrous when working concurrently (see more in Documentation/Mutability)

            bool mutability_matches;
            {
                TypeMutability target_mutability = target.MutabilityOfType(ctx);
                // cheapest part to compute
                mutability_matches = MutabilityMatches(ctx.Env.Options, inputMutability, target_mutability);

                // fixing mutability mismatch
                if (!mutability_matches
                    &&
                (matching.ForcedIgnoreMutability
                    // in case when we have two value types in hand mutability does not matter, because we copy
                    // instances and they are separate beings afterwards
                    || (!matching.MutabilityCheckRequestByData
                        && input.TargetType.IsValueType(ctx) && target.TargetType.IsValueType(ctx))))
                {
                    mutability_matches = true;
                }
            }

            if (!mutability_matches)
            {
                match = TypeMatch.Mismatched(mutability: true);
                return true;
            }
            else if (matching.LifetimeCheck && matching.TargetLifetime.Outlives(matching.InputLifetime))
            {
                match = TypeMatch.Lifetime;
                return true;
            }
            else if (distance == 0 && input.Target == target.Target)
            {
                match = TypeMatch.Same;
                return true;
            }
            else
            {
                match = TypeMatch.Substituted(distance);
                return true;
            }
        }

        internal static bool MutabilityMatches(IOptions options, TypeMutability inputMutability, TypeMutability targetMutability)
        {
            if (inputMutability.HasFlag(TypeMutability.Reassignable))
                inputMutability ^= TypeMutability.Reassignable;
            if (targetMutability.HasFlag(TypeMutability.Reassignable))
                targetMutability ^= TypeMutability.Reassignable;

            if (options.MutabilityMode == MutabilityModeOption.OnlyAssignability)
                return true;

            switch (inputMutability)
            {
                case TypeMutability.DualConstMutable: return true;

                case TypeMutability.ForceConst:
                case TypeMutability.ConstAsSource:
                    return targetMutability != TypeMutability.ForceMutable && targetMutability != TypeMutability.GenericUnknownMutability;

                case TypeMutability.ForceMutable:
                case TypeMutability.GenericUnknownMutability:
                    return targetMutability != TypeMutability.ConstAsSource && targetMutability != TypeMutability.ForceConst;

                case TypeMutability.ReadOnly: return targetMutability == TypeMutability.ReadOnly;

                default: throw new NotImplementedException();
            }
        }

        public static TypeMatch inversedTypeMatching(ComputationContext ctx, EntityInstance input, IEnumerable<TypeAncestor> targets,
            TypeMatching matching)
        {
            if (!targets.Any())
                return TypeMatch.No;

            EntityInstance target = targets.First().AncestorInstance;

            TypeMutability target_mutability = target.MutabilityOfType(ctx);

            foreach (TypeAncestor inherited_target in targets)
            {
                // please note that unlike normal type matching we reversed the types, in enum you can
                // pass base type as descendant!
                if (matchTypes(ctx, target_mutability, target.Lifetime, inherited_target.AncestorInstance,
                    input, matching, inherited_target.Distance,
                    out TypeMatch match))
                {
                    return match;
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
            TypeMutability mutability_override = TypeMutability.None;
            {
                a_dereferenced = ctx.Env.DereferencedOnce(anyTypeA, out IEntityInstance deref_a, out bool a_via_pointer);
                b_dereferenced = ctx.Env.DereferencedOnce(anyTypeA, out IEntityInstance deref_b, out bool b_via_pointer);
                if (a_dereferenced && b_dereferenced)
                {
                    TypeMutability mutability_a = anyTypeA.SurfaceMutabilityOfType(ctx);
                    TypeMutability mutability_b = anyTypeB.SurfaceMutabilityOfType(ctx);
                    if (mutability_a == mutability_b)
                        mutability_override = mutability_a;
                    else
                        mutability_override = TypeMutability.ReadOnly;

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

            HashSet<EntityInstance> set_a = type_a.Inheritance(ctx).OrderedAncestorsIncludingObject.Concat(type_a)
                            .ToHashSet(EntityInstance.CoreComparer);
            result = selectFromLowestCommonAncestorPool(ctx, type_b, set_a);
            if (result != null && a_dereferenced && b_dereferenced)
                //                result = ctx.Env.Reference(result, TypeMutability.None, null, via_pointer);
                result = ctx.Env.Reference(result, mutability_override, via_pointer);
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

            return ArgumentsMatchConstraintsOf(ctx, closedTemplate.Target.Name.Parameters, closedTemplate);
        }

        public static ConstraintMatch ArgumentsMatchConstraintsOf(ComputationContext ctx,
            IEnumerable<TemplateParameter> templateParameters, EntityInstance closedTemplate)
        {
            if (templateParameters.Count() != closedTemplate.TemplateArguments.Count)
                return ConstraintMatch.UndefinedTemplateArguments;

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
                if (!match.Passed)
                    return false;
            }

            return true;
        }
    }
}
