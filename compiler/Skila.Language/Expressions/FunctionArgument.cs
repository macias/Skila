using System;
using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Extensions;
using System.Linq;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionArgument : Node, IExpression, IIndexed, ILambdaTransfer
    {
        public static FunctionArgument Create(string nameLabel, IExpression expression)
        {
            return new FunctionArgument(nameLabel, expression);
        }
        public static FunctionArgument Create(IExpression expression)
        {
            return new FunctionArgument(null, expression);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        public EvaluationInfo Evaluation => this.Expression.Evaluation;
        public ValidationData Validation { get { return this.Expression.Validation; } set { this.Expression.Validation = value; } }
        public int DereferencedCount_LEGACY { get { return this.Expression.DereferencedCount_LEGACY; } set { this.Expression.DereferencedCount_LEGACY = value; } }
        public int DereferencingCount { get; set; }

        public string NameLabel { get; }
        public bool HasNameLabel => this.NameLabel != null;
        public bool IsComputed => this.Evaluation != null;
        private IExpression expression;
        public IExpression Expression => this.expression;
        public override IEnumerable<INode> OwnedNodes => new INode[] { Expression }.Concat(closures);
        private readonly Later<ExecutionFlow> flow;
        public ExecutionFlow Flow => this.flow.Value;
        private Option<int> index;
        public int Index
        {
            get { return index.Value; }
            private set
            {
                if (this.index.HasValue && this.index.Value != value)
                    throw new InvalidOperationException("Index is already set.");
                this.index = new Option<int>(value);
            }
        }

        private readonly List<TypeDefinition> closures;

        internal bool IsSpread => this.Expression is Spread;
        public FunctionParameter MappedTo { get; private set; }
        public ExpressionReadMode ReadMode => Expression.ReadMode;

        private FunctionArgument(string nameLabel, IExpression expression)
           : base()
        {
            this.NameLabel = nameLabel;
            this.expression = expression;

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreatePath(Expression));
        }

        internal void SetTargetParam(ComputationContext ctx, FunctionParameter param)
        {
            this.MappedTo = param;

            if (param.IsVariadic && this.IsSpread)
                this.Expression.Cast<Spread>().LiveSetup(ctx,param.Variadic);
        }
        public bool IsLValue(ComputationContext ctx)
        {
            return this.expression.IsLValue(ctx);
        }
        public override string ToString()
        {
            return (NameLabel == null ? "" : $"{NameLabel}: ") + this.Expression.ToString();
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public void Evaluate(ComputationContext ctx)
        {
            this.TrapClosure(ctx, ref this.expression);
        }

        internal void DataTransfer(ComputationContext ctx, IEntityInstance targetTypeName)
        {
            if (!this.DataTransfer(ctx, ref this.expression, targetTypeName))
                throw new Exception("Internal error");
        }

        public void Validate(ComputationContext ctx)
        {

        }

        public void SetIndex(int index)
        {
            this.Index = index;
        }
        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
