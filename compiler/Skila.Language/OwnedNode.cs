using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public abstract class OwnedNode : Node, IOwnedNode
    {
        // we share nodes in such way, that first attach sets the owner -- any following tries to attach node silently fails
        // when detaching also only owner can detach the node
        // thanks to this we don't have to clone nodes and once it is evaluated, the results is shared as well
        // on the other hand we have to track nodes manually on traversal
        public IOwnedNode Owner { get; private set; }
        public IScope Scope => Owner == null ? null : ((this.Owner as IScope) ?? this.Owner.Scope);

        protected OwnedNode() : base()
        {
        }

        protected void attachPostConstructor()
        {
            this.ChildrenNodes.WhereType<IOwnedNode>().ForEach(it => it.AttachTo(this));
        }

        public void DetachFrom(IOwnedNode owner)
        {
            if (this.Owner != owner || this.Owner == null)
                return;

            this.Owner = null;
        }
        public virtual bool AttachTo(IOwnedNode owner)
        {
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
