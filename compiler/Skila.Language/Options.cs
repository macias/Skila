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
        // todo: limit usage of this option and eventually remove it completely
        public bool TypelessVariablesDuringTests { get; set; }
        public bool ThrowOnError { get; set; } 

        public override string ToString()
        {
            return this.GetEnabledProperties().Join(", ");
        }
    }
}
