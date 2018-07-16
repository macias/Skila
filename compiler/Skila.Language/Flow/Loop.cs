using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using System;
using Skila.Language.Semantics;
using Skila.Language.Expressions;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language.Flow
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Loop : Node, IExpression, IExecutableScope, IAnchor
    {
        public static IExpression CreateFor(NameDefinition label, IEnumerable<IExpression> init,
            IExpression condition,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return create(label, init, condition, body, postStep: step, postCondition: null);
        }
        public static IExpression CreateWhile(NameDefinition label, 
            IExpression condition,
            IEnumerable<IExpression> body)
        {
            return create(label, null, condition, body, postStep: null, postCondition: null);
        }
        public static IExpression CreateWhile(
            IExpression condition,
            IEnumerable<IExpression> body)
        {
            return CreateWhile(null, condition, body);
        }
        public static IExpression CreateFor(IEnumerable<IExpression> init,
            IExpression condition,
            IEnumerable<IExpression> step,
            IEnumerable<IExpression> body)
        {
            return create(null, init, condition, body, postStep: step, postCondition: null);
        }

        public static IExpression CreateForEach(string varName, INameReference varTypeName, IExpression iterable,
            IEnumerable<IExpression> body)
        {
            string iter_name = AutoName.Instance.CreateNew("iter");
            VariableDeclaration iter_decl = VariableDeclaration.CreateStatement(iter_name, null,
                FunctionCall.Create(NameReference.Create(iterable, NameFactory.IterableGetIterator)));

            IExpression condition;
            if (varName == NameFactory.Sink)
            {
                condition = ExpressionFactory.OptionalAssignment(NameFactory.SinkReference(),
                    FunctionCall.Create(NameReference.Create(iter_name, NameFactory.IteratorNext)));
            }
            else
            {
                string elem_name = AutoName.Instance.CreateNew("elem");
                body = VariableDeclaration.CreateStatement(varName, varTypeName,
                         NameReference.Create(elem_name))
                         .Concat(body);

                condition = ExpressionFactory.OptionalDeclaration(elem_name, varTypeName,
                    FunctionCall.Create(NameReference.Create(iter_name, NameFactory.IteratorNext)));
            }

            return create(null, new[] { iter_decl },
                preCondition: condition,
                body: body,
                postStep: null, postCondition: null);
        }

        private static IExpression create(NameDefinition label,
            IEnumerable<IExpression> init,
            IExpression preCondition,
            IEnumerable<IExpression> body,
            IEnumerable<IExpression> postStep,
            IExpression postCondition)
        {
            return Block.CreateStatement((init ?? Enumerable.Empty<IExpression>())
                .Concat(new Loop(label, preCondition, body, postStep, postCondition)));
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public ExpressionReadMode ReadMode { get; }

        public NameDefinition Label { get; }
        public NameDefinition Name => this.Label;
        private IExpression preCondition;
        public IExpression PreCondition => this.preCondition;
        public IEnumerable<IExpression> PostStep { get; }
        public IEnumerable<IExpression> Body { get; }
        // same meaning as pre- version, when it fails, loop ends
        private IExpression postCondition;
        public IExpression PostCondition => this.postCondition;

        public override IEnumerable<INode> OwnedNodes => new INode[] { Label }
            .Concat(PreCondition).Concat(Body).Concat(PostStep).Concat(PostCondition).Where(it => it != null);

        private readonly Later<ExecutionFlow> flow;
        public ExecutionFlow Flow => this.flow.Value;

        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public int DereferencedCount_LEGACY { get; set; }
        public int DereferencingCount { get; set; }

        private Loop(NameDefinition label,
            IExpression preCondition,
            IEnumerable<IExpression> body,
            IEnumerable<IExpression> postStep,
            IExpression postCondition)
        {
            // each iteration is:
            // pre-condition
            // body
            // step
            // post-condition

            this.Label = label;
            this.preCondition = preCondition;
            this.PostStep = (postStep ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.Body = (body ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.postCondition = postCondition;

            this.ReadMode = ExpressionReadMode.CannotBeRead; // todo: temporary state

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreateLoop(PreCondition,
                thenPath: Body,
                postMaybes: PostStep.Concat(PostCondition)));
        }

        public override string ToString()
        {
            return Printout().ToString();
        }

        public ICode Printout()
        {
            var code = new CodeDiv(this,this.Body.ToArray()).Indent();
            code.Prepend("{").Append("}");
            code.Prepend(new CodeSpan("for (").Append(";").Append(PreCondition).Append(";").Append(PostStep," ;, ").Append(")"));
            if (PostCondition != null)
                code.Append(new CodeSpan("endfor (").Append(PostCondition).Append(")"));
            else
                code.Append(new CodeSpan("endfor"));

            return code;
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
