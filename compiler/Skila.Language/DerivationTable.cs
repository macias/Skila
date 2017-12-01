using System;
using System.Collections.Generic;
using Skila.Language.Entities;
using System.Linq;

namespace Skila.Language
{
    public sealed class DerivationTable
    {
        // derived function -> base one
        private readonly IReadOnlyDictionary<FunctionDefinition, List< FunctionDefinition>> derivedBaseMapping;

        public DerivationTable(IReadOnlyDictionary<FunctionDefinition, List< FunctionDefinition>> mapping)
        {
            this.derivedBaseMapping = mapping;
        }

        public bool TryGetSuper(ref FunctionDefinition function)
        {
            if (!this.derivedBaseMapping.TryGetValue(function, out List< FunctionDefinition> base_functions))
                return false;

            if (!base_functions.Any())
                return false;

            function = base_functions.First();
            return true;
        }
    }
}
