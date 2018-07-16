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
        public bool IsHeapInitialization => this.allocMemory == Memory.Heap;

        public IEnumerable<IExpression> Instructions => new IExpression[] { tempDeclaration, InitConstructorCall }
            .Concat(objectInitialization).Concat(outcome);

        public override IEnumerable<INode> OwnedNodes => this.Instructions;

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

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
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
            if (this.DebugId == (23, 98))
            {
                ;
            }
            if (this.Evaluation == null)
            {
                this.Evaluation = this.outcome.Evaluation;
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

            if (this.tempDeclaration.Evaluation.Aggregate.TargetType.Name.Parameters.Any() && this.InitConstructorCall.UserArguments.Any())
            {
                IEnumerable<TimedIEntityInstance> inferred = this.InitConstructorCall.Resolution
                  .InferTemplateArguments(ctx, this.tempDeclaration.Evaluation.Aggregate.TargetType).StoreReadOnly();

                if (inferred.All(it => it != null))
                {
                    foreach (var pair in inferred.SyncZip(this.tempDeclaration.Evaluation.Aggregate.TimedTemplateArguments))
                    {
                        pair.Item2.SetLifetime(ctx,pair.Item1.Lifetime);
                    }
                    foreach (var pair in inferred.SyncZip(this.tempDeclaration.Evaluation.Components.Cast<EntityInstance>().TimedTemplateArguments))
                    {
                        pair.Item2.SetLifetime(ctx, pair.Item1.Lifetime);
                    }

                    
                    /*this.allocTypeName = this.allocTypeName.Recreate(inferred.SyncZip(this.allocTypeName.TemplateArguments)
                        .Select(it => new TemplateArgument(TypeIReference.Create(it.Item1.Lifetime, it.Item2.TypeName.Name))),
                            this.allocTypeName.Binding.Match.Instance, this.allocTypeName.Binding.Match.IsLocal);

                    ctx.EvalLocalNames?.RemoveLast(this.tempDeclaration);
                    this.tempDeclaration.ReplaceInitValue(createAlloc());
                    this.tempDeclaration.Evaluated(ctx, EvaluationCall.Nested);*/
                }
            }

            // evaluate arguments first so we get lifetimes of them
            //this.InitConstructorCall.UserArguments.ForEach(it => it.Evaluated(ctx, EvaluationCall.Nested));
            this.OwnedNodes.ForEach(it => it.Evaluated(ctx, EvaluationCall.Nested));

            this.Evaluate(ctx);

        }
    }
}
