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

        public bool TryGetDerived(FunctionDefinition baseFunction,out FunctionDefinition derivedFunction)
        {
            if (!this.baseDerivedMapping.TryGetValue(baseFunction, out derivedFunction))
                return false;

            if (derivedFunction == null)
                throw new Exception("Internal error");

            return true;
        }
    }
}
