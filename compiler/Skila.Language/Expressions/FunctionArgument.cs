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
        public bool IsDereferenced { get { return this.Expression.IsDereferenced; } set { this.Expression.IsDereferenced = value; } }

        public string NameLabel { get; }
        public bool HasNameLabel => this.NameLabel != null;
        public bool IsComputed => this.Evaluation != null;
        private IExpression expression;
        public IExpression Expression => this.expression;
        public override IEnumerable<INode> OwnedNodes => new INode[] { Expression }.Concat(closures);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Expression);
        private Option<int> index;
        public int Index
        {
            get { return index.Value; }
            set
            {
                if (this.index.HasValue)
                    throw new InvalidOperationException("Index is already set.");
                this.index = new Option<int>(value);
            }
        }

        private readonly List<TypeDefinition> closures;

        public FunctionParameter MappedTo { get; set; }
        public ExpressionReadMode ReadMode => Expression.ReadMode;

        private FunctionArgument(string nameLabel, IExpression expression)
           : base()
        {
            this.NameLabel = nameLabel;
            this.expression = expression;

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
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

        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
