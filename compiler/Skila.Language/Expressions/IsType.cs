using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

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

        public override IEnumerable<INode> OwnedNodes => new INode[] { Lhs, RhsTypeName }.Where(it => it != null);
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(Lhs);

        private IsType(IExpression lhs, INameReference rhsTypeName)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.Lhs = lhs;
            this.RhsTypeName = rhsTypeName;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = Lhs.ToString() + " is " + this.RhsTypeName.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.BoolType.InstanceOf;

                if (this.Lhs.Evaluation.MatchesTarget(ctx, this.RhsTypeName.Evaluation, allowSlicing: true)== TypeMatch.Pass)
                    // checking if x (of String) is Object does not make sense
                    ctx.ErrorManager.AddError(ErrorCode.IsTypeOfKnownTypes, this);
                else if (this.RhsTypeName.Evaluation.MatchesTarget(ctx, this.Lhs.Evaluation, allowSlicing: true)!= TypeMatch.Pass)
                    // we cannot check if x (of Int) is String because it is illegal
                    ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);
                else
                    foreach (EntityInstance instance in this.Lhs.Evaluation.Enumerate())
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
    }
}
