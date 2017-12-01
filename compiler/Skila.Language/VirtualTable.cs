using System;
using System.Collections.Generic;
using Skila.Language.Entities;
using System.Linq;

namespace Skila.Language
{
    public sealed class VirtualTable
    {
        public bool IsPartial { get; }
        // base function -> derived one
        private readonly IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> baseDerivedMapping;

        public VirtualTable(IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping, bool isPartial)
        {
            this.baseDerivedMapping = mapping;
            this.IsPartial = isPartial;
        }

        public bool TryGetDerived(ref FunctionDefinition function)
        {
            if (!this.baseDerivedMapping.TryGetValue(function, out FunctionDefinition derived_func))
                return false;

            if (derived_func == null)
                throw new Exception("Internal error");

            function = derived_func;
            return true;
        }
    }
}
