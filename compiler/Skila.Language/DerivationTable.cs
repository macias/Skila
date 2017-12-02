using System;
using System.Collections.Generic;
using Skila.Language.Entities;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class DerivationTable
    {
        // derived function -> base implementation function
        private readonly IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> derivedBaseMapping;

        public DerivationTable(ComputationContext ctx, IReadOnlyDictionary<FunctionDefinition, List<FunctionDefinition>> mapping)
        {
            this.derivedBaseMapping = compute(ctx, mapping);
        }

        // we look for base functions in two directions
        // horizontally -- in given type (direct) parents
        // vertically -- in primary ancestors (primary parent of primary parent of ...)
        // we need to find single base function (implementation) to say we have unambiguous base function
        // when we have multiple targets -- well, we have a problem (read more below)

        private static Dictionary<FunctionDefinition, FunctionDefinition> compute(ComputationContext ctx,
            IReadOnlyDictionary<FunctionDefinition, List<FunctionDefinition>> mapping)
        {
            if (!mapping.Any())
                return null;

            var derived_base_mapping = new Dictionary<FunctionDefinition, FunctionDefinition>();

            TypeDefinition current_type = mapping.First().Key.OwnerType();
            List<TypeDefinition> parents = current_type.Inheritance.MinimalParentsWithObject
                // primary parent will be present in primary ancestors sequence
                .Skip(1)
                .Select(it => it.TargetType)
                .ToList();
            List<TypeDefinition> primary_ancestors = current_type.InstanceOf.PrimaryAncestors(ctx)
                .Select(it => it.TargetType)
                .ToList();

            foreach (KeyValuePair<FunctionDefinition, List<FunctionDefinition>> entry in mapping)
            {
                Dictionary<TypeDefinition, FunctionDefinition> base_impls = entry.Value
                    .Where(it => !it.IsDeclaration)
                    .ToDictionary(it => it.OwnerType(), it => it);

                if (!base_impls.Any())
                    continue;
                else if (base_impls.Count == 1)
                {
                    derived_base_mapping.Add(entry.Key, base_impls.Single().Value);
                    continue;
                }

                var reachable_impls = new List<FunctionDefinition>();

                foreach (TypeDefinition parent in parents)
                    if (base_impls.TryGetValue(parent, out FunctionDefinition func))
                        reachable_impls.Add(func);

                // we look only for the first one, because here we have a chain of derivation
                foreach (TypeDefinition ancestor in primary_ancestors)
                    if (base_impls.TryGetValue(ancestor, out FunctionDefinition func))
                    {
                        reachable_impls.Add(func);
                        break;
                    }

                // more than one implementation is incorrect because we don't know which one to choose
                // zero is simply not covered -- because it means there are somewhere among ancestor several implementations
                // and in such case with have two scenarios
                // (a) those implementations are derived from each other -- so it would be good to implement finding the lowest one
                // (b) they are not derived from each other -- in such case we don't know which one to pick and we should give user
                //     an error
                if (reachable_impls.Count != 1)
                    throw new NotImplementedException("Handle it -- either give user error or pick the right (?) implementation");

                derived_base_mapping.Add(entry.Key, reachable_impls.Single());
            }

            return derived_base_mapping;
        }

        public bool TryGetSuper(FunctionDefinition function,out FunctionDefinition super)
        {
            if (this.derivedBaseMapping == null)
            {
                super = null;
                return false;
            }

            if (!this.derivedBaseMapping.TryGetValue(function, out super))
                return false;

            return true;
        }
    }
}
