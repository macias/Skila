using System.Collections.Generic;

namespace Skila.Language
{
    public interface ISurfable : IOwnedNode
    {
        bool IsSurfed { get; set; }
        
        void Surf(ComputationContext ctx);
    }

}
