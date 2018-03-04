using Skila.Language.Semantics;

namespace Skila.Language
{
    public interface IValidable : INode
    {
        void Validate(ComputationContext ctx);
    }

}
