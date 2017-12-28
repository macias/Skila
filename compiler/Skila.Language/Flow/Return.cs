﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Return : Expression, IFunctionExit
    {
        public static Return Create(IExpression value = null)
        {
            return new Return(value);
        }

        private IExpression expr;
        public IExpression Expr => this.expr;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Expr }.Where(it => it != null);

        private Return(IExpression value) : base(ExpressionReadMode.CannotBeRead)
        {
            this.expr = value;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = "return";
            if (Expr != null)
                result += " " + Expr.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.Expr;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 160)
                {
                    ;
                }

                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (func == null)
                    ctx.ErrorManager.AddError(ErrorCode.ReturnOutsideFunction, this);
                else
                {
                    if (func.IsResultTypeNameInfered)
                    {
                        if (this.Expr == null)
                            func.AddResultTypeCandidate(ctx.Env.UnitType.InstanceOf.NameOf);
                        else
                            func.AddResultTypeCandidate(this.Expr.Evaluation.Components.NameOf);
                    }
                    else
                    {
                        IEntityInstance func_result = func.ResultTypeName.Evaluation.Components;

                        if (this.Expr == null)
                        {
                            if (!ctx.Env.IsUnitType(func_result))
                                ctx.ErrorManager.AddError(ErrorCode.EmptyReturn, this);
                        }
                        else
                        {
                            this.DataTransfer(ctx, ref this.expr, func_result);
                        }
                    }
                }

                this.Evaluation = ctx.Env.UnitEvaluation;
            }
        }
    }
}
