using System.Collections.Generic;
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

        private IExpression value;
        public IExpression Value => this.value;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Value }.Where(it => it != null);

        private Return(IExpression value) : base(ExpressionReadMode.CannotBeRead)
        {
            this.value = value;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = "return";
            if (Value != null)
                result += " " + Value.ToString();
            return result;
        }

        public override bool IsReadingValueOfNode( IExpression node)
        {
            return node == this.Value;
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
                    IEntityInstance func_result = func.ResultTypeName.Evaluated(ctx);

                    if (this.Value == null)
                    {
                        if (!ctx.Env.IsVoidType(func_result))
                            ctx.ErrorManager.AddError(ErrorCode.TypeMismatch, this);
                    }
                    else
                    {
                        this.DataTransfer(ctx, ref this.value, func_result);
                    }
                }

                this.Evaluation = ctx.Env.VoidType.InstanceOf;
            }
        }
    }
}
