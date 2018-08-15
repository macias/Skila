using System.Collections.Generic;

namespace Skila.Language.Expressions
{
    public interface IFunctionArgumentsProvider : IOwnedNode
    {
        IReadOnlyList<FunctionArgument> UserArguments { get; }
        INameReference RequestedOutcomeTypeName { get; }
    }
}