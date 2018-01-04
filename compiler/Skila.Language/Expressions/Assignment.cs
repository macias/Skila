using System;
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
    public sealed class Assignment : Expression, ILambdaTransfer
    {
        public static IExpression CreateStatement(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue);
        }
        public static IExpression CreateStatement(IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            return create(ExpressionReadMode.CannotBeRead, lhs, rhsValue);
        }
        public static IExpression CreateExpression(IExpression lhs, IExpression rhsValue)
        {
            return create(ExpressionReadMode.ReadRequired, lhs, rhsValue);
        }
        public static IExpression CreateExpression(IEnumerable<IExpression> lhs, IEnumerable<IExpression> rhsValue)
        {
            return create(ExpressionReadMode.ReadRequired, lhs, rhsValue);
        }
        private static IExpression create(ExpressionReadMode readMode, IExpression lhs, IExpression rhsValue)
        {
            if (lhs is FunctionCall call && call.IsIndexer)
            {
                if (readMode != ExpressionReadMode.CannotBeRead)
                    throw new NotImplementedException();

                return call.ConvertIndexerIntoSetter(rhsValue);
            }
            else
                return new Assignment(readMode, lhs, rhsValue);
        }
        private static IExpression create(ExpressionReadMode readMode, IEnumerable<IExpression> lhsExpr, IEnumerable<IExpression> rhsValue)
        {
            if (lhsExpr.Count() == 1 && rhsValue.Count() == 1)
            {
                return create(readMode, lhsExpr.Single(), rhsValue.Single());
            }
            else if (lhsExpr.Count() == 1)
            {
                return create(readMode, lhsExpr.Single(), ExpressionFactory.Tuple(rhsValue.ToArray()));
            }
            else
            {
                string rhs_temp = AutoName.Instance.CreateNew("par_ass");
                var code = new List<IExpression>();
                code.Add(VariableDeclaration.CreateStatement(rhs_temp, null, ExpressionFactory.Tuple(rhsValue.ToArray())));
                int i = 0;
                foreach (IExpression lhs in lhsExpr)
                {
                    code.Add(Assignment.CreateStatement(lhs, NameReference.Create(rhs_temp, NameFactory.TupleItemName(i))));
                    ++i;
                }

                if (readMode != ExpressionReadMode.CannotBeRead)
                    code.Add(NameReference.Create(rhs_temp));

                return Block.Create(readMode, code);
            }
        }


        // we don't override IsLValue in Assignment and VariableDeclaration 
        // because while technically it is lvalue, it is ridiculous to see
        // (var x = y) = 7;
        // it looks more like a bug in code and besides, if anything it should be chain assignment
        // var x = (y = 7);

        public IExpression Lhs { get; }
        private IExpression rhsValue;
        public IExpression RhsValue => this.rhsValue;

        private readonly List<TypeDefinition> closures;

        public override IEnumerable<INode> OwnedNodes => new INode[] { RhsValue, Lhs }.Concat(closures).Where(it => it != null);

        private Assignment(ExpressionReadMode readMode, IExpression lhs, IExpression rhsValue)
            : base(readMode)
        {
            this.Lhs = lhs;
            this.rhsValue = rhsValue;

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
            ctx.ValAssignTracker?.Assigned(this.Lhs);

            if (!ctx.Env.Options.DiscardingAnyExpressionDuringTests && this.Lhs.IsSink() && !(this.RhsValue is FunctionCall))
                ctx.AddError(ErrorCode.DiscardingNonFunctionCall, this);

            RhsValue.ValidateValueExpression(ctx);

            IEntityVariable decl = this.Lhs.TryGetTargetEntity<IEntityVariable>(out NameReference lhs_name);
            if (decl != null && lhs_name.HasThisPrefix)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (!func.Modifier.HasMutable && !func.IsAnyConstructor())
                {
                    ctx.AddError(ErrorCode.AlteringInstanceInImmutableMethod, this);
                }
            }
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.TrapClosure(ctx, ref this.rhsValue);

                this.DataTransfer(ctx, ref this.rhsValue, Lhs.Evaluation.Components);

                this.Evaluation = this.RhsValue.Evaluation;

                {
                    IEntityVariable lhs_var = this.Lhs.TryGetTargetEntity<IEntityVariable>(out NameReference dummy);
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
                            if (sameTargets(this.Lhs, this.RhsValue))
                                ctx.AddError(ErrorCode.SelfAssignment, this);
                        }
                    }
                }

                if (this.Lhs.DebugId.Id == 1110)
                {
                    ;
                }
                if (!this.Lhs.IsLValue(ctx))
                    ctx.AddError(ErrorCode.AssigningRValue, this.Lhs);
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
