using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using Skila.Language.Semantics;
using Skila.Language.Expressions;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Loop : Node, IExpression, IExecutableScope, IAnchor
    {
        public static Loop CreateFor(NameDefinition label, IEnumerable<IExpression> init,
            IExpression condition,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return new Loop(label, init, condition, body, postStep: step, postCondition: null);
        }
        public static Loop CreateFor(IEnumerable<IExpression> init,
            IExpression condition,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return new Loop(null, init, condition, body, postStep: step, postCondition: null);
        }

        public static Loop CreateForEach(string varName, INameReference varTypeName, IExpression iterable,
            IEnumerable<IExpression> body)
        {
            // todo: remove this limitation once we have typedefs and/or optional declaration
            if (varTypeName == null)
                throw new ArgumentNullException("Temporarily null type is not supported");

            string iter_name = AutoName.Instance.CreateNew("iter");
            VariableDeclaration iter_decl = VariableDeclaration.CreateStatement(iter_name, null,
                FunctionCall.Create(NameReference.Create(iterable, NameFactory.IterableGetIterator)));

            //IExpression condition;
            string elem_name;
            VariableDeclaration elem_decl;
            if (varName == NameFactory.Sink)
            {
                elem_name = NameFactory.Sink;
                elem_decl = null;
                //condition = ExpressionFactory.OptionalAssignment(NameFactory.SinkReference(),
                  //  FunctionCall.Create(NameReference.Create(iter_name, NameFactory.IteratorNext)));
            }
            else
            {
                elem_name = AutoName.Instance.CreateNew("elem");
                elem_decl = VariableDeclaration.CreateStatement(elem_name, varTypeName, Undef.Create(), EntityModifier.Reassignable);
                body = VariableDeclaration.CreateStatement(varName, varTypeName,
                         NameReference.Create(elem_name))
                         .Concat(body);

                //condition = ExpressionFactory.OptionalDeclaration(elem_name, varTypeName,
                  //  () => FunctionCall.Create(NameReference.Create(iter_name, NameFactory.IteratorNext)));
            }

            // todo: once we have optional declarations use them here 
            return new Loop(null, new[] { iter_decl
                , elem_decl
            }
            .Where(it => it != null)
            ,
                            preCondition: ExpressionFactory.OptionalAssignment(NameReference.Create(elem_name),
                    FunctionCall.Create(NameReference.Create(iter_name, NameFactory.IteratorNext))),
  //              preCondition: condition,
                body: body,
                postStep: null, postCondition: null);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public ExpressionReadMode ReadMode { get; }

        public NameDefinition Label { get; }
        public NameDefinition Name => this.Label;
        public IEnumerable<IExpression> Init { get; }
        private IExpression preCondition;
        public IExpression PreCondition => this.preCondition;
        public IEnumerable<IExpression> PostStep { get; }
        public IEnumerable<IExpression> Body { get; }
        // same meaning as pre- version, when it fails, loop ends
        private IExpression postCondition;
        public IExpression PostCondition => this.postCondition;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Label }.Concat(Init).Concat(PreCondition).Concat(Body).Concat(PostStep).Concat(PostCondition).Where(it => it != null);
        private readonly Later<ExecutionFlow> flow;
        public ExecutionFlow Flow => this.flow.Value;

        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public int DereferencedCount_LEGACY { get; set; }
        public int DereferencingCount { get; set; }

        private Loop(NameDefinition label,
            IEnumerable<IExpression> init,
            IExpression preCondition,
            IEnumerable<IExpression> body,
            IEnumerable<IExpression> postStep,
            IExpression postCondition)
        {
            // execute init expression, then iterate, each iteration is
            // pre-condition
            // body
            // step
            // post-condition

            this.Label = label;
            this.Init = (init ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.preCondition = preCondition;
            this.PostStep = (postStep ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.Body = (body ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.postCondition = postCondition;

            this.ReadMode = ExpressionReadMode.CannotBeRead; // todo: temporary state

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreateLoop(Init.Concat(PreCondition), thenPath: Body, postMaybes: PostStep.Concat(PostCondition)));
        }

        public override string ToString()
        {
            string result = "loop ...";
            return result;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return this.PreCondition == node || this.PostCondition == node;
        }

        public void Validate(ComputationContext ctx)
        {
        }
        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = ctx.Env.UnitEvaluation;

                this.DataTransfer(ctx, ref this.preCondition, ctx.Env.BoolType.InstanceOf);
                this.DataTransfer(ctx, ref this.postCondition, ctx.Env.BoolType.InstanceOf);
            }
        }
        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }
    }
}
