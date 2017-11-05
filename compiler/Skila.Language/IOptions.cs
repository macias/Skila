namespace Skila.Language
{
    public interface IOptions
    {
        // in C# you cannot call "some_object.StaticMember", it has to be "SomeType.StaticMember"
        bool StaticMemberOnlyThroughTypeName { get; }
    }
}