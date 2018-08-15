using System.Collections.Generic;

namespace Skila.Language
{
    public interface INode
    {
#if DEBUG
        DebugId DebugId { get; }
#endif

        IEnumerable<INode> ChildrenNodes { get; }
    }   
}
