using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using System;
using Skila.Language.Printout;

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

        public FunctionCall TailCallOptimization { get; private set; }

        private Return(IExpression value) : base(ExpressionReadMode.CannotBeRead)
        {
            this.expr = value;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            var code = new CodeSpan("return");
            if (Expr != null)
                code.Append(" ").Append(Expr.Printout());
            return code;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.Expr;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.DebugId==   (18, 377))
            {
                ;
            }

            if (this.Evaluation == null)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (func == null)
                    ctx.ErrorManager.AddError(ErrorCode.ReturnOutsideFunction, this);
                else
                {
                    if (func.IsResultTypeNameInfered)
                    {
                        if (this.Expr == null)
                            func.AddResultTypeCandidate(ctx.Env.UnitType.InstanceOf);
                        else
                            func.AddResultTypeCandidate(this.Expr.Evaluation.Components);
                    }
                    else
                    {
                        IEntityInstance func_result = func.ResultParameter.TypeName.Evaluation.Components;

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

                // https://stackoverflow.com/questions/7563981/why-isnt-g-tail-call-optimizing-while-gcc-is
                // http://www.drdobbs.com/tackling-c-tail-calls/184401756
                if (this.Expr is FunctionCall call && call.IsRecall()
                    && !func.Parameters.Any(it => ctx.Env.IsReferenceOfType(it.TypeName.Evaluation.Components)))
                    this.TailCallOptimization = call;

                this.Evaluation = ctx.Env.UnitEvaluation;
            }
        }
    }
}
