using System.Collections.Generic;

namespace Skila.Language.Expressions
{
    public interface IFunctionArgumentsProvider : INode
    {
        IReadOnlyList<FunctionArgument> UserArguments { get; }
        INameReference RequestedOutcomeTypeName { get; }
    }
}