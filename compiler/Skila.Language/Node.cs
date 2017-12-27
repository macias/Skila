using System.Collections.Generic;

namespace Skila.Language
{
    public abstract class Node : INode
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        public abstract IEnumerable<INode> OwnedNodes { get; }
        // we share nodes in such way, that first attach sets the owner -- any following tries to attach node silently fails
        // when detaching also only owner can detach the node
        // thanks to this we don't have to clone nodes and once it is evaluated, the results is shared as well
        // on the other hand we have to track nodes manually on traversal
        public INode Owner { get; private set; }
        public IScope Scope => Owner == null ? null : ((this.Owner as IScope) ?? this.Owner.Scope);

        public void DetachFrom(INode owner)
        {
            if (this.DebugId.Id== 3183)
            {
                ;
            }

            if (this.Owner != owner || this.Owner == null)
                return;

            this.Owner = null;
        }
        public virtual bool AttachTo(INode owner)
        {
            if (this.DebugId.Id == 3183)
            {
                ;
            }

            if (owner == null || this.Owner == owner)
                return false;
            else if (this.Owner == null)
            {
                this.Owner = owner;
            }
            else if (this.Owner != owner)
            {
                return false;
            }

            return true;
        }
    }

}
