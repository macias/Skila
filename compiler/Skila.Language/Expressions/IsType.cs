using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Printout;
using Skila.Language.Semantics;
using Skila.Language.Tools;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class IsType : Expression
    {
        public static IsType Create(IExpression lhs, INameReference rhsTypeName)
        {
            return new IsType(lhs, rhsTypeName);
        }

        public IExpression Lhs { get; }
        public INameReference RhsTypeName { get; }

        public override IEnumerable<INode> ChildrenNodes => new IOwnedNode[] { Lhs, RhsTypeName }.Where(it => it != null);
        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;

        private IsType(IExpression lhs, INameReference rhsTypeName)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Lhs = lhs;
            this.RhsTypeName = rhsTypeName;

            this.attachPostConstructor();

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreatePath(Lhs));
        }
        public override string ToString()
        {
            return this.Printout().ToString();
        }

        public override ICode Printout()
        {
            return new CodeSpan(Lhs).Append(" is ").Append(RhsTypeName);
        }
        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                this.Evaluation = new EvaluationInfo(ctx.Env.BoolType.InstanceOf.Build(Lifetime.Create(this)));
        }

        public static bool MatchTypes(ComputationContext ctx, IEntityInstance lhsTypeInstance, IEntityInstance rhsTypeInstance)
        {
            TypeMatch lhs_rhs_match = lhsTypeInstance.MatchesTarget(ctx, rhsTypeInstance,
                    TypeMatching.Create(duckTyping: false, allowSlicing: true).WithIgnoredMutability(true));
            return lhs_rhs_match.Passed;
        }
        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            if (this.RhsTypeName.Evaluation.Components is EntityInstance rhs_type)
            {
                if (MatchTypes(ctx, this.Lhs.Evaluation.Components, this.RhsTypeName.Evaluation.Components))
                    // checking if x (of String) is Object does not make sense
                    ctx.ErrorManager.AddError(ErrorCode.IsTypeOfKnownTypes, this);
                else
                {
                    if (!TypeMatcher.InterchangeableTypes(ctx, this.Lhs.Evaluation.Components, this.RhsTypeName.Evaluation.Components))
                        ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);

                    foreach (EntityInstance instance in this.Lhs.Evaluation.Components.EnumerateAll())
                    {
                        if (!instance.Target.IsType())
                            continue;

                        // this error is valid as long we don't allow mixes of value types, like "Int|Bool"
                        if (!instance.TargetType.AllowSlicedSubstitution)
                        {
                            // value types are known in advance (in compilation time) so checking their types
                            // in runtime does not make sense
                            ctx.ErrorManager.AddError(ErrorCode.IsTypeOfKnownTypes, this);
                            break;
                        }
                    }
                }
            }
            else
                ctx.AddError(ErrorCode.TestingAgainstTypeSet, this.RhsTypeName);
        }

    }
}
