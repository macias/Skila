namespace Skila.Language.Entities
{
    public interface IMember : IEntity
    {
        bool IsMemberUsed { get; }
        void SetIsMemberUsed();
    }
}
