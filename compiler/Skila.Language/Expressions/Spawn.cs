using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Spawn : Expression
    {
        public static Spawn Create(FunctionCall call)
        {
            return new Spawn(call);
        }

        public FunctionCall Call { get; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Call }.Where(it => it != null);

        private Spawn(FunctionCall call)
            : base(ExpressionReadMode.CannotBeRead)
        {
            this.Call = call;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"spawn {Call}";
            return result;
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.UnitEvaluation;

                foreach (FunctionArgument arg in this.Call.Arguments)
                    if (!arg.Evaluation.Components.IsImmutableType(ctx))
                        ctx.AddError(ErrorCode.CannotSpawnWithMutableArgument, arg);

                if (this.Call.Resolution.MetaThisArgument != null && !this.Call.Resolution.MetaThisArgument.Evaluated(ctx).IsImmutableType(ctx))
                    ctx.AddError(ErrorCode.CannotSpawnOnMutableContext, this.Call.Callee);

            }
        }
    }
}
