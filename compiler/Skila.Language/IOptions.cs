namespace Skila.Language
{
    public interface IOptions
    {
        // in C# you cannot call "some_object.StaticMember", it has to be "SomeType.StaticMember"
        bool StaticMemberOnlyThroughTypeName { get; }
        // you can substitute prototype with any type as long all methods are covered
        bool InterfaceDuckTyping { get; }
        // when true, inner scope variable can shadow outer scope variable (with the same name)
        bool ScopeShadowing { get; }
        // in Skila we have "super" to call the base function of the current one, so supporting "base.foo()" for cross-calling
        // other base functions is rather not welcome because promote probably incorrect code
        bool BaseReferenceEnabled { get; }
    }
}