using System.Collections.Generic;

namespace Skila.Language
{
    public abstract class Node : INode
    {
#if DEBUG
        public DebugId DebugId { get; }
#endif

        public abstract IEnumerable<INode> ChildrenNodes { get; }

        protected Node()
        {
#if DEBUG
            DebugId = new DebugId(this.GetType());
#endif
        }
    }
}
