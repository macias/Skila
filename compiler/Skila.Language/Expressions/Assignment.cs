﻿using System;
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
    public sealed class Assignment : Expression
    {
        public static Assignment CreateStatement(IExpression lhs, IExpression rhsValue)
        {
            return new Assignment(ExpressionReadMode.CannotBeRead, lhs, rhsValue);
        }
        public static Assignment CreateExpression(IExpression lhs, IExpression rhsValue)
        {
            return new Assignment(ExpressionReadMode.ReadRequired, lhs, rhsValue);
        }

        // we don't override IsLValue in Assignment and VariableDeclaration 
        // because while technically it is lvalue, it is ridiculous to see
        // (var x = y) = 7;
        // it looks more like a bug in code and besides, if anything it should be chain assignment
        // var x = (y = 7);

        public IExpression Lhs { get; }
        private IExpression rhsValue;
        public IExpression RhsValue => this.rhsValue;

        public override IEnumerable<INode> OwnedNodes => new INode[] { RhsValue, Lhs }.Where(it => it != null);

        private Assignment(ExpressionReadMode readMode, IExpression lhs, IExpression rhsValue)
            : base(readMode)
        {
            this.Lhs = lhs;
            this.rhsValue = rhsValue;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = Lhs.ToString() + " = " + this.RhsValue.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return true;
        }

        public override void Validate( ComputationContext ctx)
        {
            ctx.ValAssignTracker?.Assigned(this.Lhs);

        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = Lhs.Evaluated(ctx);

                this.DataTransfer(ctx, ref this.rhsValue, this.Evaluation);

                RhsValue.ValidateValueExpression(ctx);

                {
                    IEntityVariable lhs_var = this.Lhs.TryGetEntityVariable();
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
                            IEntityVariable rhs_var = this.RhsValue.TryGetEntityVariable();
                            if (lhs_var == rhs_var)
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
    }
}
