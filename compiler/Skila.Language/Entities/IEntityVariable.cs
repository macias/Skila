namespace Skila.Language.Entities
{
    public interface IEntityVariable : IEntity
    {
        bool HasValueOnDeclaration { get; }
        bool IsDeclaration { get; }
    }
}
