using System.Collections.Generic;

namespace Skila.Language.Expressions
{
    public interface IFunctionArgumentsProvider : INode
    {
        IReadOnlyList<FunctionArgument> Arguments { get; }
        INameReference RequestedOutcomeTypeName { get; }
    }
}