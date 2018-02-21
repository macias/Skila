using NaiveLanguageTools.Common;
using System.Linq;

namespace Skila.Language
{
    public sealed class Options : IOptions
    {
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

        public bool RelaxedMode { get; set; }
        private bool allowEmptyFieldTypeNames;
        public bool AllowEmptyFieldTypeNames
        {
            get { return this.allowEmptyFieldTypeNames || this.RelaxedMode; }
            set { this.allowEmptyFieldTypeNames = value; }
        }
        private bool allowNamedSelf;
        public bool AllowNamedSelf
        {
            get { return this.allowNamedSelf || this.RelaxedMode; }
            set { this.allowNamedSelf = value; }
        }


        public bool StaticMemberOnlyThroughTypeName { get; set; }
        public bool InterfaceDuckTyping { get; set; }
        public bool ScopeShadowing { get; set; }
        public bool ReferencingBase { get; set; }
        public bool DiscardingAnyExpressionDuringTests { get; set; }
        public bool GlobalVariables { get; set; }
        public bool MiniEnvironment { get; set; }
        public bool AllowInvalidMainResult { get; set; }
        public bool AllowProtocols { get; set; }
        public bool AllowRealMagic { get; set; }
        public bool AtomicPrimitivesMutable { get; set; }

        public override string ToString()
        {
            return this.GetEnabledProperties().Join(", ");
        }
    }
}
