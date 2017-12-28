using System.Collections.Generic;
using Skila.Language;

namespace Skila.Interpreter
{
    // we need this stuff just because async methods in C# do not allow ref
    internal interface ICallContext
    {
        ObjectData[] FunctionArguments { get; set; }
        IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }
        ObjectData ThisArgument { get; set; }
    }
}