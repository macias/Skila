using System;
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
    public sealed class FunctionParameter : Node, IEntityVariable, IIndexed, ILocalBindable, ISurfable
    {
        public static FunctionParameter Create(string name, INameReference typeName, Variadic variadic,
            IExpression defaultValue,
             bool isNameRequired,
             ExpressionReadMode usageMode = ExpressionReadMode.ReadRequired)
        {
            return new FunctionParameter(usageMode, name, typeName, variadic, defaultValue, isNameRequired: isNameRequired);
        }
        public static FunctionParameter Create(string name, INameReference typeName,
            ExpressionReadMode usageMode = ExpressionReadMode.ReadRequired)
        {
            return new FunctionParameter(usageMode, name, typeName, Variadic.None, null, isNameRequired: false);
        }

        public bool IsNameRequired { get; }
        public bool IsOptional => this.DefaultValue != null;
        public Variadic Variadic { get; }
        public bool IsVariadic => this.Variadic != Variadic.None;
        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;
        private readonly EntityInstanceCache instancesCache;
        public NameDefinition Name { get; }
        public EntityModifier Modifier { get; }
        public INameReference ElementTypeName { get; }
        public INameReference TypeName { get; }
        private IExpression defaultValue;
        public IExpression DefaultValue => this.defaultValue;

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, DefaultValue }.Where(it => it != null);

        public bool IsComputed { get; private set; }

        public ExpressionReadMode UsageMode { get; }
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

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

        public bool IsSurfed { get; set; }

        private FunctionParameter(ExpressionReadMode readMode, string name, INameReference typeName, Variadic variadic,
            IExpression defaultValue, bool isNameRequired)
        {
            this.UsageMode = readMode;
            this.Modifier = EntityModifier.None;
            this.Name = NameDefinition.Create(name);
            this.IsNameRequired = isNameRequired;
            this.Variadic = variadic;

            this.ElementTypeName = typeName;
            if (this.IsVariadic)
                this.TypeName = NameFactory.ReferenceTypeReference(NameFactory.ISequenceTypeReference(typeName, overrideMutability: MutabilityFlag.ForceMutable));
            else
                this.TypeName = typeName;

            this.defaultValue = defaultValue;

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(null, MutabilityFlag.ConstAsSource,
                translation: TemplateTranslation.Create(this)));

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string variadic_str = this.Variadic.ToString();
            if (variadic_str != "")
                variadic_str = " " + variadic_str;
            return this.Name + (this.IsNameRequired ? ":" : "")
                + $" {this.ElementTypeName}{variadic_str}" + (IsOptional ? " = " + DefaultValue.ToString() : "");
        }

        public void Surf(ComputationContext ctx)
        {
            compute(ctx);
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                compute(ctx);

            this.DataTransfer(ctx, ref this.defaultValue, this.Evaluation.Components);

            if (this.IsOptional)
            {
                if (this.DefaultValue.IsUndef())
                    ctx.AddError(ErrorCode.InitializationWithUndef, this.DefaultValue);

                this.DefaultValue.IsRead = true;
            }

            if (this.IsVariadic && !this.Variadic.HasValidLimits)
                ctx.ErrorManager.AddError(ErrorCode.InvalidVariadicLimits, this);

            this.IsComputed = true;
        }

        private void compute(ComputationContext ctx)
        {
            this.Evaluation = this.TypeName.Evaluation ?? throw new Exception("Internal error");
        }

        public void Validate(ComputationContext ctx)
        {
            this.ValidateReferenceAssociatedReference(ctx);
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, MutabilityFlag overrideMutability, 
            TemplateTranslation translation)
        {
            return this.instancesCache.GetInstance(arguments, overrideMutability, translation);
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        internal bool NOT_USED_CounterpartParameter(INode thisScope, FunctionParameter other, INode otherScope)
        {
            // todo: add relative checking so foo<T>(t T) will be equal to bar<X>(x X)
            if (!this.Variadic.Equals(other.Variadic))
                return false;
            if (!EntityBareNameComparer.Instance.Equals(this.Name, other.Name))
                return false;
            if (this.TypeName.Evaluation.Components.IsSame(other.TypeName.Evaluation.Components, jokerMatchesAll: true))
                return false;

            return true;
        }

        public void SetIndex(int index)
        {
            this.Index = index;
        }
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
