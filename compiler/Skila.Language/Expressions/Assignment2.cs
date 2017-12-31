/*using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Assignment2 : Expression, ILambdaTransfer
    {
        public static IExpression CreateStatement(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, new[] { lhs }, new[] { rhsValue });
        }
        public static IExpression CreateStatement(IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue);
        }
        public static IExpression CreateExpression(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.ReadRequired, new[] { lhs }, new[] { rhsValue });
        }
        private static IExpression create(ExpressionReadMode readMode, IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            if (lhs is FunctionCall call && call.IsIndexer)
            {
                if (readMode != ExpressionReadMode.CannotBeRead)
                    throw new NotImplementedException();

                return call.ConvertIndexerIntoSetter(rhsValue);
            }
            else
                return new Assignment2(readMode, lhs, rhsValue);
        }

        // we don't override IsLValue in Assignment and VariableDeclaration 
        // because while technically it is lvalue, it is ridiculous to see
        // (var x = y) = 7;
        // it looks more like a bug in code and besides, if anything it should be chain assignment
        // var x = (y = 7);

        public IReadOnlyList<IExpression> Lhs { get; }
        private List<IExpression> rhsValue;
        public IEnumerable<IExpression> RhsValue => this.rhsValue;

        private readonly List<TypeDefinition> closures;

        public override IEnumerable<INode> OwnedNodes => RhsValue.Select(it => it.Cast<INode>()).Concat(Lhs).Concat(closures)
            .Where(it => it != null);

        private Assignment2(ExpressionReadMode readMode, IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
            : base(readMode)
        {
            this.Lhs = lhs.StoreReadOnlyList();
            this.rhsValue = rhsValue.ToList();

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = Lhs.ToString() + " = " + this.RhsValue.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public override void Validate(ComputationContext ctx)
        {
            foreach (IExpression lhs_expr in this.Lhs)
            {
                ctx.ValAssignTracker?.Assigned(lhs_expr);

                if (!lhs_expr.IsLValue(ctx))
                    ctx.AddError(ErrorCode.AssigningRValue, lhs_expr);               
            }

            foreach (Tuple<IExpression, IExpression> lhs_rhs in this.Lhs.SyncZip(this.RhsValue))
            {
                if (!ctx.Env.Options.DiscardingAnyExpressionDuringTests && lhs_rhs.Item1.IsSink() && !(lhs_rhs.Item2 is FunctionCall))
                    ctx.AddError(ErrorCode.DiscardingNonFunctionCall, this);

                {
                    IEntityVariable lhs_var = lhs_rhs.Item1.TryGetTargetEntity<IEntityVariable>(out NameReference dummy);
                    if (lhs_var != null)
                    {
                        FunctionDefinition current_func = this.EnclosingScope<FunctionDefinition>();
                        bool can_reassign = lhs_var.Modifier.HasReassignable ||
                            (current_func != null && current_func.OwnerType() == lhs_var.OwnerType()
                            && (current_func.IsInitConstructor() || current_func.IsZeroConstructor()));

                        if (!can_reassign)
                            ctx.AddError(ErrorCode.CannotReassignReadOnlyVariable, this);
                        else
                        {
                            if (sameTargets(lhs_rhs.Item1, lhs_rhs.Item2))
                                ctx.AddError(ErrorCode.SelfAssignment, this);
                        }
                    }
                }
            }

            foreach (IExpression rhs_expr in this.RhsValue)
                rhs_expr.ValidateValueExpression(ctx);
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                for (int i = 0; i < this.Lhs.Count; ++i)
                {
                    IExpression rhs = this.rhsValue[i];

                    this.TrapClosure(ctx, ref rhs);

                    this.DataTransfer(ctx, ref rhs, this.Lhs[i].Evaluation.Components);

                    this.rhsValue[i] = rhs;
                }

                this.Evaluation = this.RhsValue.Evaluation;

             
            }
        }

        private static bool sameTargets(IExpression lhs, IExpression rhs)
        {
            NameReference lhs_ref;
            IEntity lhs_target = lhs.TryGetTargetEntity<IEntity>(out lhs_ref);
            if (lhs_target == null)
                return false;
            NameReference rhs_ref;
            IEntity rhs_target = rhs.TryGetTargetEntity<IEntity>(out rhs_ref);
            if (lhs_target != rhs_target)
                return false;

            if (lhs_ref.Prefix == null && rhs_ref.Prefix == null)
                return true;
            else
                return sameTargets(lhs_ref.Prefix, rhs_ref.Prefix);
        }

        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
*/