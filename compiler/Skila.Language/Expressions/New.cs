using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Printout;

namespace Skila.Language.Expressions
{
    // specialized (and simplified) version of Block:
    // { temp = alloc ; temp.init(args) ; temp }
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class New : Expression, IExecutableScope, ICustomComputable
    {
        internal static New Create(string tempName, NameReference allocTypeName,
            Memory allocMemory,
            TypeMutability allocMutability, FunctionCall init,
            IEnumerable<IExpression> objectInitialization, NameReference outcome)
        {
            return new New(tempName, allocTypeName, allocMemory, allocMutability, init, objectInitialization, outcome);
        }

        private readonly VariableDeclaration tempDeclaration;
        public FunctionCall InitConstructorCall { get; }
        private readonly IEnumerable<IExpression> objectInitialization;
        private readonly NameReference outcome;

        private readonly Memory allocMemory;
        private readonly TypeMutability allocMutability;
        private NameReference allocTypeName;
        private Lifetime forcedLifetime;

        public bool IsHeapInitialization => this.allocMemory == Memory.Heap;

        public IEnumerable<IExpression> Instructions => new IExpression[] { tempDeclaration, InitConstructorCall }
            .Concat(objectInitialization).Concat(outcome);

        public override IEnumerable<INode> ChildrenNodes => this.Instructions;

        private New(string tempName, NameReference allocTypeName,
            Memory allocMemory,
            TypeMutability allocMutability,
            FunctionCall init,
            IEnumerable<IExpression> objectInitialization, NameReference outcome)
            : base(ExpressionReadMode.ReadRequired)
        {
            this.allocTypeName = allocTypeName;
            this.allocMemory = allocMemory;
            this.allocMutability = allocMutability;

            this.tempDeclaration = VariableDeclaration.CreateStatement(tempName, null, createAlloc());
            this.InitConstructorCall = init;
            this.objectInitialization = objectInitialization.StoreReadOnly();
            this.outcome = outcome;

            this.attachPostConstructor();
        }

        private Alloc createAlloc()
        {
            return Alloc.Create(this.allocTypeName, this.allocMemory, this.allocMutability);
        }

        public override string ToString()
        {
            return Printout().ToString();
        }

        public override ICode Printout()
        {
            var code = new CodeSpan($"new {(this.IsHeapInitialization ? "*" : "")}").Append(allocTypeName).Append("(")
                .Append(InitConstructorCall.UserArguments, ",")
                .Append(")");
            if (this.objectInitialization.Any())
                code.Append("{").Append(this.objectInitialization, ",").Append("}");

            return code;
        }


        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.forcedLifetime != null)
                    this.Evaluation = this.outcome.Evaluation.PromoteLifetime(ctx, this.forcedLifetime);
                else
                    this.Evaluation = this.outcome.Evaluation.PromotLifetime(ctx, this);
            }
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.outcome;
        }

        public void CustomEvaluate(ComputationContext ctx)
        {
            this.tempDeclaration.Evaluated(ctx, EvaluationCall.Nested);
            this.InitConstructorCall.Evaluated(ctx, EvaluationCall.Nested);

            if (this.tempDeclaration.Evaluation.Aggregate.TargetType.Name.Parameters.Any()
                && this.InitConstructorCall.UserArguments.Any())
            {
                IEnumerable<IEntityInstance> inferred = this.InitConstructorCall.Resolution
                  .InferTemplateArguments(ctx, this.tempDeclaration.Evaluation.Aggregate.TargetType).StoreReadOnly();


                foreach (IEntityInstance instance in inferred.Where(it => it != null))
                {
                    if (!ctx.Env.IsReferenceOfType(instance))
                        continue;

                    this.forcedLifetime = instance.Lifetime.AsAttached().Shorter(forcedLifetime);
                }
            }

            if (this.InitConstructorCall.Resolution?.AttachmentLifetime != null)
                forcedLifetime = this.InitConstructorCall.Resolution.AttachmentLifetime.Shorter(forcedLifetime);

            this.ChildrenNodes.ForEach(it => it.Evaluated(ctx, EvaluationCall.Nested));

            this.Evaluate(ctx);

        }
    }
}
