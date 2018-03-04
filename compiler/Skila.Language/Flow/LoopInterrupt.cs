using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;
using System;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language.Flow
{
    /// <summary>
    /// break, continue
    /// </summary>
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class LoopInterrupt : Node, IExpression, IFlowJump, ILoopInterrupt
    {
        public static LoopInterrupt CreateBreak(string label = null)
        {
            return new LoopInterrupt(label == null ? (LabelReference)null : LabelReference.CreateLocal(label), isBreak: true);
        }
        public static LoopInterrupt CreateContinue(string label = null)
        {
            return new LoopInterrupt(label == null ? (LabelReference)null : LabelReference.CreateLocal(label), isBreak: false);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public IAnchor AssociatedLoop { get; private set; }
        public LabelReference Label { get; }
        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public int DereferencingCount { get; set; }
        public int DereferencedCount_LEGACY { get; set; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { Label }.Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.Empty;
        public ExpressionReadMode ReadMode => ExpressionReadMode.CannotBeRead;
        public bool IsBreak { get; }

        private LoopInterrupt(LabelReference label, bool isBreak) : base()
        {
            this.IsBreak = isBreak;
            this.Label = label;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = "break";
            if (Label != null)
                result += " " + Label.ToString();
            return result;
        }

        public void Validate(ComputationContext ctx)
        {
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.UnitEvaluation;

                if (this.Label == null)
                    this.AssociatedLoop = this.EnclosingScope<Loop>();
                else
                {
                    this.AssociatedLoop = this.Label.Binding.Cast<IAnchor>();
                }

                Loop loop = this.EnclosingScope<Loop>();
                if (loop == null)
                    ctx.ErrorManager.AddError(ErrorCode.LoopControlOutsideLoop, this);
            }
        }
        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
    }
}
