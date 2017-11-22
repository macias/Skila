using Skila.Language.Entities;
using Skila.Language.Expressions;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Skila.Language
{
    // the purpose of this type is to detect references to variables which go outside the lambda
    // we need to grab them to make a closure

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    internal sealed class LambdaTrap
    {
        private readonly Dictionary<VariableDefiniton, VariableDefiniton> escapingVariableToFieldMapping;
        public IEnumerable<VariableDefiniton> Fields => this.escapingVariableToFieldMapping.Values;

        public LambdaTrap()
        {
            this.escapingVariableToFieldMapping = new Dictionary<VariableDefiniton, VariableDefiniton>();
        }

        internal VariableDefiniton HijackEscapingReference(VariableDefiniton localVariable)
        {
            // we replace here the escaping local variable (or rather variable that breaks lambda barrier)
            // with field of the type which soon will be created
            if (localVariable == null)
                throw new Exception("Internal error");

            VariableDefiniton field;
            if (!this.escapingVariableToFieldMapping.TryGetValue(localVariable, out field))
            {
                field = VariableDefiniton.CreateStatement(localVariable.Name.Name, localVariable.Evaluation.NameOf, Undef.Create());
                this.escapingVariableToFieldMapping.Add(localVariable, field);
            }

            return field;
        }
    }
}
