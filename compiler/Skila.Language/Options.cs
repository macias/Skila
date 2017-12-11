namespace Skila.Language
{
    public sealed class Options : IOptions
    {
        public bool StaticMemberOnlyThroughTypeName { get; set; }
        public bool InterfaceDuckTyping { get; set; }

        public bool ScopeShadowing { get; set; }
        public bool BaseReferenceEnabled { get; set; }
        public bool AllowDiscardingAnyExpressionDuringTests { get; set; }
    }
}
