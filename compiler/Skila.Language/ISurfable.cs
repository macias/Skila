using System.Collections.Generic;

namespace Skila.Language
{
    public interface ISurfable : INode
    {
        bool IsSurfed { get; set; }
        
        void Surf(ComputationContext ctx);
    }

}
