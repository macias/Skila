using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using Skila.Language.Tools;

namespace Skila.Language.Expressions
{
    // this is similar to comma operator (in Skila implemented as block expression) but it does not create scope
    // the reason for introducing this was optional declaration structure and desire to avoid open-block or something like this
    // thus specialized type
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Chain : Expression
    {
        public static Chain Create(IEnumerable<IExpression> instructions)
        {
            return new Chain(instructions);
        }

        public IEnumerable<IExpression> Instructions { get; }

        public override IEnumerable<INode> OwnedNodes => this.Instructions;

        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private Chain(IEnumerable<IExpression> instructions)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Instructions = instructions.StoreReadOnly();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() =>
            {
                return ExecutionFlow.CreatePath(instructions);
            });
        }

        public override string ToString()
        {
            int count = this.Instructions.Count();
            return (this.Instructions.FirstOrDefault()?.Printout()?.ToString() ?? "") + (count > 1 ? $"...{{{count}}}" : "");
        }

        public override ICode Printout()
        {
            return new CodeSpan().Append("{").Append(" ").Append(this.Instructions, " ;; ").Append(" ").Append("}");
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.Instructions.Last();
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.Instructions.Last().Evaluation;
            }
        }
    }
}
