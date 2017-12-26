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
        private readonly Dictionary<FunctionDefinition, FunctionDefinition> baseDerivedMapping;

        public VirtualTable(Dictionary<FunctionDefinition, FunctionDefinition> mapping, bool isPartial)
        {
            this.baseDerivedMapping = mapping;
            this.IsPartial = isPartial;
        }

        public VirtualTable(bool isPartial)
        {
            IsPartial = isPartial;
            this.baseDerivedMapping = new Dictionary<FunctionDefinition, FunctionDefinition>();
        }

        public bool TryGetDerived(FunctionDefinition baseFunction,out FunctionDefinition derivedFunction)
        {
            if (!this.baseDerivedMapping.TryGetValue(baseFunction, out derivedFunction))
                return false;

            if (derivedFunction == null)
                throw new Exception("Internal error");

            return true;
        }

        internal void OverrideWith(VirtualTable table)
        {
            if (table == null)
                return;

            foreach (KeyValuePair<FunctionDefinition,FunctionDefinition> entry in table.baseDerivedMapping)
                this.Update(entry.Key,entry.Value);
        }

        internal void Update(FunctionDefinition baseFunc, FunctionDefinition derivedFunc)
        {
            if (baseDerivedMapping.ContainsKey(baseFunc))
                baseDerivedMapping[baseFunc] = derivedFunc;
            else
                baseDerivedMapping.Add(baseFunc, derivedFunc);
        }
    }
}
