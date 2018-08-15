using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using Skila.Language.Tools;
using Skila.Language.Printout;

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

        public override IEnumerable<INode> ChildrenNodes => new INode[] { Call }.Where(it => it != null);

        private Spawn(FunctionCall call)
            : base(ExpressionReadMode.CannotBeRead)
        {
            this.Call = call;

            this.attachPostConstructor();
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan("spawn ").Append(Call);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.UnitEvaluation;

                foreach (FunctionArgument arg in this.Call.UserArguments)
                    if (arg.Evaluation.Components.MutabilityOfType(ctx) != TypeMutability.ConstAsSource)
                        ctx.AddError(ErrorCode.CannotSpawnWithMutableArgument, arg);

                if (this.Call.Resolution.MetaThisArgument != null
                    && this.Call.Resolution.MetaThisArgument.Evaluated(ctx, EvaluationCall.AdHocCrossJump).MutabilityOfType(ctx) != TypeMutability.ConstAsSource)
                    ctx.AddError(ErrorCode.CannotSpawnOnMutableContext, this.Call.Callee);

            }
        }
    }
}
