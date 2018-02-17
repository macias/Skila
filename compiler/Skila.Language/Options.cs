using NaiveLanguageTools.Common;
using System.Linq;

namespace Skila.Language
{
    public sealed class Options : IOptions
    {
        public bool StaticMemberOnlyThroughTypeName { get; set; }
        public bool InterfaceDuckTyping { get; set; }

        public bool ScopeShadowing { get; set; }
        public bool ReferencingBase { get; set; }
        public bool DiscardingAnyExpressionDuringTests { get; set; }
        public bool GlobalVariables { get; set; }
        public bool RelaxedMode { get; set; }
        private bool debugThrowOnError;
        public bool DebugThrowOnError
        {
            get
            {
                return debugThrowOnError;
                //return false;
            }
            set { debugThrowOnError = value; }
        }
        public bool MiniEnvironment { get; set; }
        public bool AllowInvalidMainResult { get; set; }
        public bool AllowProtocols { get; set; }

        public override string ToString()
        {
            return this.GetEnabledProperties().Join(", ");
        }
    }
}
