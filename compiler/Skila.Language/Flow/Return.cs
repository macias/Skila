using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using System;

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
            string result = "return";
            if (Expr != null)
                result += " " + Expr.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.Expr;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            escapeReferenceValidation(ctx);

        }

        private void escapeReferenceValidation(ComputationContext ctx)
        {
            // in case of references we have to make sure they won't outlive their sources, consider such examples:
            // def foo(x ref int) ref int
            //   return x;
            // end
            // this is ok, because assuming input is valid, we can legally return the reference

            // def bar() ref int
            //   x int = 5;
            //   return x;
            // end
            // this is wrong, we would return reference (address) to the data living on stack, at the moment
            // of return stack is deleted and we have address to phantom data


            // TODO: currently it is disaster, it is the effect of lack of time, implement it properly
            if (this.Expr != null && ctx.Env.IsReferenceOfType(this.Expr.Evaluation.Components))
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (func != null && ctx.Env.IsReferenceOfType(func.ResultTypeName.Evaluation.Components))
                {
                    if (this.Expr is AddressOf address_of)
                    {
                        IEntityVariable entity = address_of.Expr.TryGetTargetEntity<IEntityVariable>(out NameReference name_ref);

                        if (entity is VariableDeclaration decl)
                        {
                            throw new NotImplementedException();
                            //if (!address_of.Expr.TargetsCurrentInstanceMember(out IMember dummy))
                            //  ctx.AddError(ErrorCode.EscapingReference, this.Expr);
                        }
                        else if (entity is Property prop)
                        {
                            if (!address_of.Expr.TargetsCurrentInstanceMember(out IMember dummy))
                                ctx.AddError(ErrorCode.EscapingReference, this.Expr);
                        }
                        else
                            ctx.AddError(ErrorCode.EscapingReference, this.Expr);
                    }
                    else
                    {
                        IEntityVariable entity = this.Expr.TryGetTargetEntity<IEntityVariable>(out NameReference name_ref);

                        if (entity is FunctionParameter)
                        {
                            ; // ok
                        }
                        else if (entity is VariableDeclaration decl)
                        {
                            // todo: this is incorrect, we need to add lifetime control, in this case we return local variable
                            // but the source of this variable could be local (error) or external (like function parameter -- OK)
                            ;
                        }
                        else if (this.Expr is FunctionCall call)
                        {
                            if (!call.Name.TargetsCurrentInstanceMember(out IMember dummy1) &&
                                !call.Name.Prefix.TargetsCurrentInstanceMember(out IMember dummy2))
                                ctx.AddError(ErrorCode.EscapingReference, this.Expr);
                        }
                        else
                        {
                            throw new NotImplementedException();
                            //ctx.AddError(ErrorCode.EscapingReference, this.Expr);
                        }
                    }
                }
            }
        }

        public override void Evaluate(ComputationContext ctx)
        {
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
