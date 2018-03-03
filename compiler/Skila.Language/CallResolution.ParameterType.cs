using Skila.Language.Extensions;
using System;
using System.Diagnostics;

namespace Skila.Language
{
    public sealed partial class CallResolution
    {
        [DebuggerDisplay("{GetType().Name} {ToString()}")]
        private sealed class ParameterType
        {
            internal static ParameterType Create(FunctionParameter param,
                IEntityInstance objectInstance,
                EntityInstance targetFunctionInstance)
            {
                IEntityInstance elem_instance = orderedTranslatation(param.ElementTypeName.Evaluation.Components,
                    objectInstance, targetFunctionInstance);

                IEntityInstance type_instance = orderedTranslatation(param.TypeName.Evaluation.Components,
                    objectInstance, targetFunctionInstance);

                return new ParameterType(elementTypeInstance: elem_instance, typeInstance: type_instance);
            }

            public IEntityInstance ElementTypeInstance { get; }
            public IEntityInstance TypeInstance { get; }

            private ParameterType(IEntityInstance elementTypeInstance, IEntityInstance typeInstance)
            {
                this.ElementTypeInstance = elementTypeInstance;
                this.TypeInstance = typeInstance;
            }

            public override string ToString()
            {
                return this.ElementTypeInstance + (this.ElementTypeInstance != this.TypeInstance ? "..." : "");
            }

        }
    }
}
