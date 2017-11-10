using System;
using System.Collections.Generic;
using Skila.Language.Entities;

namespace Skila.Interpreter
{
    sealed class VirtualTable
    {
        // base function -> derived one
        private readonly IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping;

        public VirtualTable(IReadOnlyDictionary<FunctionDefinition, FunctionDefinition> mapping)
        {
            this.mapping = mapping;
        }

        public FunctionDefinition GetDerived(FunctionDefinition baseFunction)
        {
            if (this.mapping.TryGetValue(baseFunction, out FunctionDefinition derived))
                return derived;
            else
                throw new Exception("Internal error");

        }
    }
}
