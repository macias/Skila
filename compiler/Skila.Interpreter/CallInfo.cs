using System;
using System.Collections.Generic;
using Skila.Language;
using Skila.Language.Entities;

namespace Skila.Interpreter
{
    internal sealed class CallInfo : ICallContext
    {
        public FunctionDefinition FunctionTarget { get; }

        public ObjectData[] FunctionArguments { get; set; }
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; set; }
        public ObjectData ThisArgument { get; set; }

        public CallInfo(FunctionDefinition func)
        {
            this.FunctionTarget = func;
        }

        internal void Apply(ref ExecutionContext ctx)
        {
            ctx.FunctionArguments = this.FunctionArguments;
            ctx.TemplateArguments = this.TemplateArguments;
            ctx.ThisArgument = this.ThisArgument;
        }
    }
}