using Skila.Language.Semantics;
using System.Collections.Generic;

namespace Skila.Language
{
    public interface ISurfable : INode
    {
        bool IsSurfed { get; set; }
        IEnumerable<ISurfable> Surfables { get; }

        void Surf(ComputationContext ctx);
    }

}
