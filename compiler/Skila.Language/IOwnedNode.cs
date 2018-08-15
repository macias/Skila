namespace Skila.Language
{
    public interface IOwnedNode : INode
    {
        IOwnedNode Owner { get; }
        IScope Scope { get; }

        bool AttachTo(IOwnedNode owner);
        void DetachFrom(IOwnedNode owner);
    }
}
