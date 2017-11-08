﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Comparers;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionParameter : Node, IEntityVariable, IIndexed
    {
        public static FunctionParameter Create(string name, INameReference typeName, Variadic variadic,
            IExpression defaultValue,
             bool isNameRequired)
        {
            return new FunctionParameter(name, typeName, variadic, defaultValue, isNameRequired: isNameRequired);
        }

        public bool IsNameRequired { get; }
        public bool IsOptional => this.DefaultValue != null;
        public Variadic Variadic { get; }
        public bool IsVariadic => this.Variadic != Variadic.None;
        private readonly Lazy<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;
        public NameDefinition Name { get; }
        public EntityModifier Modifier { get; }
        public INameReference TypeName { get; }
        private IExpression defaultValue;
        public IExpression DefaultValue => this.defaultValue;

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, DefaultValue }.Where(it => it != null);

        public bool IsComputed => this.Evaluation != null;

        public IEntityInstance Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        private Option<int> index;
        public int Index
        {
            get { return index.Value; }
            set
            {
                if (this.index.HasValue && this.index.Value!=value)
                    throw new InvalidOperationException("Index is already set.");
                this.index = new Option<int>(value);
            }
        }

        private FunctionParameter(string name, INameReference typeName, Variadic variadic, IExpression defaultValue, bool isNameRequired)
        {
            this.Modifier = EntityModifier.None;
            this.Name = NameDefinition.Create(name);
            this.IsNameRequired = isNameRequired;
            this.TypeName = typeName;
            this.defaultValue = defaultValue;
            this.Variadic = variadic;

            this.instanceOf = new Lazy<EntityInstance>(() => EntityInstance.RAW_CreateUnregistered(this, null));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            return this.Name + (this.IsNameRequired ? ":" : "") + $" {this.TypeName} {Variadic}" + (IsOptional ? " = " + DefaultValue.ToString() : "");
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.TypeName.Evaluated(ctx);

                this.DataTransfer(ctx, ref this.defaultValue, this.Evaluation);

                if (this.IsOptional)
                {
                    if (this.DefaultValue.IsUndef())
                        ctx.AddError(ErrorCode.InitializationWithUndef, this.DefaultValue);

                    this.DefaultValue.IsRead = true;
                }

                if (this.IsVariadic && !this.Variadic.HasValidLimits)
                    ctx.ErrorManager.AddError(ErrorCode.InvalidVariadicLimits, this);
            }
        }

        public void Validate( ComputationContext ctx)
        {
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments)
        {
            return this.InstanceOf;
        }

        public bool IsReadingValueOfNode( IExpression node)
        {
            return false;
        }

        internal bool NOT_USED_CounterpartParameter(INode thisScope, FunctionParameter other, INode otherScope)
        {
            // todo: add relative checking so foo<T>(t T) will be equal to bar<X>(x X)
            if (!this.Variadic.Equals(other.Variadic))
                return false;
            if (!EntityBareNameComparer.Instance.Equals(this.Name, other.Name))
                return false;
            if (this.TypeName.Evaluation.IsSame(other.TypeName.Evaluation, jokerMatchesAll: true))
                return false;

            return true;
        }

        /*internal FunctionParameter Clone()
        {
            return new FunctionParameter(this.Name.Name, this.TypeName.Clone(), this.Variadic, this.DefaultValue?.Clone(), IsNameRequired);
        }*/

        /* public static bool CanDifferentiateByUsage(FunctionParameter p1, FunctionParameter p2)
         {
             // to tell the difference we need different names (quite obvious)
             return p1.Name != p2.Name
                 && (p1.IsNameRequired || p2.IsNameRequired) // plus at least one name required
                 && (!p1.IsOptional || !p2.IsOptional);      // and at least one non-optional param

             // example -- foo(x: Int = 5) vs foo(y Int)
             // foo() -- using optional one
             // foo(7) -- using the one without named parameter
         }*/
    }
}
