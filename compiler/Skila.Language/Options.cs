namespace Skila.Language
{
    public sealed class Options : IOptions
    {
        public bool StaticMemberOnlyThroughTypeName { get; set; }
        public bool InterfaceDuckTyping { get; set; }
    }
}
