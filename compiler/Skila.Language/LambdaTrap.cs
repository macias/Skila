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
        private readonly Dictionary<VariableDeclaration, VariableDeclaration> escapingVariableToFieldMapping;
        public IEnumerable<VariableDeclaration> Fields => this.escapingVariableToFieldMapping.Values;

        public LambdaTrap()
        {
            this.escapingVariableToFieldMapping = new Dictionary<VariableDeclaration, VariableDeclaration>();
        }

        internal IEntity HijackEscapingReference(VariableDeclaration localVariable)
        {
            // we replace here the escaping local variable (or rather variable that breaks lambda barrier)
            // with field of the type which soon will be created
            if (localVariable == null)
                throw new Exception("Internal error");

            VariableDeclaration field;
            if (!this.escapingVariableToFieldMapping.TryGetValue(localVariable, out field))
            {
                field = VariableDeclaration.CreateStatement(localVariable.Name.Name, localVariable.Evaluation.Components.NameOf,
                    Undef.Create());
                this.escapingVariableToFieldMapping.Add(localVariable, field);
            }

            return field;
        }
    }
}
