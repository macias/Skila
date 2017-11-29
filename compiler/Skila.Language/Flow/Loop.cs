using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using Skila.Language.Semantics;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Loop : Node, IExpression, IExecutableScope, IAnchor
    {
        public static Loop CreateFor(NameDefinition label, IEnumerable<IExpression> init,
            IExpression preCheck,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return new Loop(label, init, preCheck, step, body, postCheck: null);
        }
        public static Loop CreateFor(IEnumerable<IExpression> init,
            IExpression preCheck,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return new Loop(null, init, preCheck, step, body, postCheck: null);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public ExpressionReadMode ReadMode { get; }

        public NameDefinition Name { get; }
        public IEnumerable<IExpression> Init { get; }
        private IExpression preCheck;
        public IExpression PreCheck => this.preCheck;
        public IEnumerable<IExpression> Step { get; }
        public IEnumerable<IExpression> Body { get; }
        private IExpression postCheck;
        public IExpression PostCheck => this.postCheck;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Name, PreCheck, PostCheck }.Concat(Init).Concat(Step).Concat(Body).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreateLoop(Init.Concat(PreCheck), maybePath: Body, postMaybes: Step.Concat(PostCheck));

        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsDereferenced { get; set; }
        public bool IsDereferencing { get; set; }

        private Loop(NameDefinition label,
            IEnumerable<IExpression> init,
            IExpression preCheck,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body,
            IExpression postCheck)
        {
            this.Name = label;
            this.Init = (init ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.preCheck = preCheck;
            this.Step = (step ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.Body = (body ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.postCheck = postCheck;

            this.ReadMode = ExpressionReadMode.CannotBeRead; // temporary state

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string result = "loop ...";
            return result;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return this.PreCheck == node || this.PostCheck == node;
        }

        public void Validate(ComputationContext ctx)
        {
        }
        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.VoidEvaluation;

                this.DataTransfer(ctx, ref this.preCheck, ctx.Env.BoolType.InstanceOf);
                this.DataTransfer(ctx, ref this.postCheck, ctx.Env.BoolType.InstanceOf);
            }
        }
        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
    }
}
