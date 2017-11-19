using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Flow;
using Skila.Language.Semantics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class Expression : Node, IExpression
    {
        public bool IsComputed => this.Evaluation != null;

        public bool IsDereferenced { get; set; }
        public IEntityInstance Evaluation { get; protected set; }
        public ValidationData Validation { get; set; }

        public ExpressionReadMode ReadMode { get; }
        public virtual ExecutionFlow Flow => ExecutionFlow.CreatePath(OwnedNodes.WhereType<IExpression>());

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue && this.isRead!=value) throw new Exception("Internal error"); this.isRead = value; } }

        protected Expression(ExpressionReadMode readMode)
        {
            this.ReadMode = readMode;
        }

        public abstract bool IsReadingValueOfNode( IExpression node);
        public abstract void Evaluate(ComputationContext ctx);

        public virtual bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
        public virtual void Validate(ComputationContext ctx)
        {

        }

    }
}
