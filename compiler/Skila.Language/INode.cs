using System.Collections.Generic;

namespace Skila.Language
{
    public interface INode 
    {
#if DEBUG
        DebugId DebugId { get; }
#endif

        INode Owner { get; }
        IScope Scope { get; }
        IEnumerable<INode> OwnedNodes { get; }

        bool AttachTo(INode owner);
        void DetachFrom(INode owner);
    }
}
