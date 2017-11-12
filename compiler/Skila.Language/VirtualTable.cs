using System;
using System.Collections.Generic;
using Skila.Language.Entities;

namespace Skila.Language
{
    public sealed class VirtualTable
    {
        // base function -> derived one
        private readonly IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping;

        public VirtualTable(IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping)
        {
            this.mapping = mapping;
        }

        public bool TryGetDerived(ref FunctionDefinition function)
        {
            if (!this.mapping.TryGetValue(function, out FunctionDefinition derived))
                return false;

            function = derived;
            return true;
        }

        internal bool HasDerived(FunctionDefinition baseFunction)
        {
            return this.mapping.ContainsKey(baseFunction);
        }
    }
}
