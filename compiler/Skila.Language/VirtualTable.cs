using System;
using System.Collections.Generic;
using Skila.Language.Entities;

namespace Skila.Language
{
    public sealed class VirtualTable
    {
        // base function -> derived one
        private readonly IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping;
        public bool IsPartial { get; }
        public VirtualTable(IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping, bool isPartial)
        {
            this.mapping = mapping;
            this.IsPartial = isPartial;
        }

        public bool TryGetDerived(ref FunctionDefinition function)
        {
            if (!this.mapping.TryGetValue(function, out FunctionDefinition derived))
                return false;

            if (derived == null)
                throw new Exception("Internal error");

            function = derived;
            return true;
        }
    }
}
