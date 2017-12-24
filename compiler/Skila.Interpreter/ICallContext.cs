using System.Collections.Generic;
using Skila.Language;

namespace Skila.Interpreter
{
    internal interface ICallContext
    {
        ObjectData[] FunctionArguments { get; set; }
        IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }
        ObjectData ThisArgument { get; set; }
    }
}