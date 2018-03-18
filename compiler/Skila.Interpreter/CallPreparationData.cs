using System.Collections.Generic;
using NaiveLanguageTools.Common;

namespace Skila.Interpreter
{
    internal struct CallPreparationData
    {
        public Variant<object, ExecValue, CallInfo> Prep { get; }
        public IEnumerable<ArgumentGroup> ArgGroups { get; }

        private CallPreparationData(Variant<object, ExecValue, CallInfo> prep, IEnumerable<ArgumentGroup> args)
        {
            this.Prep = prep;
            this.ArgGroups = args;
        }

        public CallPreparationData(ExecValue prep) : this(new Variant<object, ExecValue, CallInfo>(prep),null)
        {
        }

        public CallPreparationData(CallInfo prep, IEnumerable<ArgumentGroup> args) 
            : this(new Variant<object, ExecValue, CallInfo>(prep), args)
        {
        }

    }
}