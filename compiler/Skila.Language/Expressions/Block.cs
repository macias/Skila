using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Block : Expression, IExecutableScope
    {
        public static Block Create(ExpressionReadMode readMode, IEnumerable<IExpression> body)
        {
            return new Block(readMode, body);
        }
        public static Block CreateStatement(IEnumerable<IExpression> body)
        {
            return new Block(ExpressionReadMode.CannotBeRead, body);
        }
        public static Block CreateStatement()
        {
            return new Block(ExpressionReadMode.CannotBeRead, body: null);
        }
        public static Block CreateExpression(IEnumerable<IExpression> body)
        {
            return new Block(ExpressionReadMode.ReadRequired, body);
        }

        internal FunctionCall constructorChainCall { get; private set; } // used in constructors
        private IExpression zeroConstructorCall;

        private readonly IReadOnlyCollection<IExpression> instructions;
        public IEnumerable<IExpression> Instructions => new[] { constructorChainCall, zeroConstructorCall }
            .Where(it => it != null)
            .Concat(this.instructions);

        public override IEnumerable<INode> OwnedNodes => Instructions.Select(it => it.Cast<INode>());

        private Block(ExpressionReadMode readMode, IEnumerable<IExpression> body) : base(readMode)
        {
            this.instructions = (body ?? Enumerable.Empty<IExpression>()).StoreReadOnly();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return this.Instructions.FirstOrDefault()?.ToString() ?? "";
        }
        public override bool IsReadingValueOfNode(IExpression node)
        {
            return this.Instructions.LastOrDefault() == node
                && (this.ReadMode == ExpressionReadMode.ReadRequired
                    || (this.Owner is IExpression owner_expr && owner_expr.IsReadingValueOfNode(this)));
        }
        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                IExpression last = this.Instructions.LastOrDefault();
                this.Evaluation = last?.Evaluation ?? ctx.Env.VoidEvaluation;
            }
        }

        internal void SetZeroConstructorCall(FunctionCall call)
        {
            if (this.zeroConstructorCall != null)
                throw new Exception("Internal error");

            this.zeroConstructorCall = call;
            call.AttachTo(this);
        }
        internal void SetConstructorChainCall(FunctionCall call)
        {
            if (this.constructorChainCall != null)
                throw new Exception("Internal error");

            this.constructorChainCall = call;
            call.AttachTo(this);
        }
    }
}
