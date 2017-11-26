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

        private readonly List<IExpression> instructions;
        public IEnumerable<IExpression> Instructions => this.instructions;

        public override IEnumerable<INode> OwnedNodes => Instructions.Select(it => it.Cast<INode>());

        private Block(ExpressionReadMode readMode, IEnumerable<IExpression> body) : base(readMode)
        {
            this.instructions = (body ?? Enumerable.Empty<IExpression>()).ToList();

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

        internal void Prepend(IExpression expression)
        {
            this.instructions.Insert(0, expression);
            expression.AttachTo(this);
        }
    }
}
