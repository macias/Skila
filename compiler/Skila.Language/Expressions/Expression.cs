﻿using NaiveLanguageTools.Common;
using Skila.Language.Printout;
using Skila.Language.Semantics;
using Skila.Language.Tools;
using System;
using System.Diagnostics;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class Expression : OwnedNode, IExpression
    {
        public bool IsComputed => this.Evaluation != null;

        public int DereferencingCount { get; set; }
        public int DereferencedCount_LEGACY { get; set; }
        public EvaluationInfo Evaluation { get; protected set; }
        public ValidationData Validation { get; set; }

        private Option<ExpressionReadMode> readMode;
        protected bool isReadModeSet => this.readMode.HasValue;
        public ExpressionReadMode ReadMode => this.readMode.Value;
        private readonly Later<ExecutionFlow> flow;
        public virtual ExecutionFlow Flow => this.flow.Value;

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue && this.isRead != value) throw new Exception("Internal error"); this.isRead = value; } }

        protected Expression(Option<ExpressionReadMode> readMode)
        {
            this.readMode = readMode;
            this.flow = Later.Create(() => ExecutionFlow.CreatePath(ChildrenNodes.WhereType<IExpression>()));
        }
        protected Expression(ExpressionReadMode readMode) : this(new Option<ExpressionReadMode>(readMode))
        { 
        }

        public abstract bool IsReadingValueOfNode(IExpression node);
        public abstract void Evaluate(ComputationContext ctx);
        public abstract ICode Printout();

        public virtual bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
        public virtual void Validate(ComputationContext ctx)
        {

        }

        protected void setReadMode(ExpressionReadMode readMode)
        {
            if (this.isReadModeSet)
                throw new Exception();

            this.readMode = new Option<ExpressionReadMode>(readMode);
        }
    }
}
