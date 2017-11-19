using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class VariableDeclaration : Expression, IEntityVariable,ILambdaTransfer
    {
        public static VariableDeclaration CreateStatement(string name, INameReference typeName, IExpression initValue, EntityModifier modifier = null)
        {
            return new VariableDeclaration(modifier, ExpressionReadMode.CannotBeRead, name, typeName, initValue);
        }
        public static VariableDeclaration CreateExpression(string name, INameReference typeName, IExpression initValue)
        {
            return new VariableDeclaration(EntityModifier.None, ExpressionReadMode.ReadRequired, name, typeName, initValue);
        }

        private readonly Lazy<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;
        public NameDefinition Name { get; }
        public INameReference TypeName { get; }
        private IExpression initValue;
        private IExpression autoFieldDefaultInit;
        public IExpression InitValue => this.initValue;
        private readonly List<TypeDefinition> closures;

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, InitValue }
            .Where(it => it != null)
            .Concat(closures);
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(InitValue);
        public EntityModifier Modifier { get; }

        private VariableDeclaration(EntityModifier modifier, ExpressionReadMode readMode, string name,
            INameReference typeName, IExpression initValue)
            : base(readMode)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Modifier = modifier ?? EntityModifier.None;
            this.Name = NameDefinition.Create(name);
            this.TypeName = typeName;
            this.initValue = initValue;

            this.instanceOf = new Lazy<EntityInstance>(() => EntityInstance.RAW_CreateUnregistered(this, null));

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            string result = $"{Name} {this.TypeName}";
            if (this.InitValue != null)
                result += $" = {this.InitValue}";
            return result;
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments)
        {
            return this.InstanceOf;
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.InitValue;
        }

        public bool IsField()
        {
            return this.OwnerType() != null;
        }

        private bool isGlobalVariable()
        {
            return this.Scope.IsNamespace();
        }

        public IExpression DetachFieldInitialization()
        {
            if (this.InitValue == null)
            {
                // we have to give target for name, because this could be property field, and it is in scope
                // of a property not enclosed type, so from constructor such field is invisible
                NameReference field_name = this.Name.CreateNameReference(this.InstanceOf);

                // we need to save it for later to change the errors, user does not see this call, but she/he
                // sees the field
                this.autoFieldDefaultInit = FunctionCall.Create(NameReference.Create(field_name, NameFactory.InitConstructorName));
                return this.autoFieldDefaultInit;
            }
            else if (!this.InitValue.IsUndef())
            {
                NameReference field_name = this.Name.CreateNameReference(this.InstanceOf);

                this.initValue.DetachFrom(this);
                var init = FunctionCall.Create(NameReference.Create(field_name,
                    NameFactory.InitConstructorName), FunctionArgument.Create(this.initValue));
                this.initValue = null;
                return init;
            }
            else
                return null;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 8812)
                {
                    ;
                }

                this.TrapClosure(ctx,ref this.initValue);

                IEntityInstance init_eval = InitValue?.Evaluation;

                IEntityInstance tn_eval = this.TypeName?.Evaluation;
                if (tn_eval != null && InitValue == null && (this.IsField() || this.isGlobalVariable()))
                {
                    if (this.TypeName.TryGetSingleType(out NameReference type_name, out EntityInstance type_instance))
                    {
                        TypeDefinition type_def = type_instance.TargetType;
                        type_def.Evaluated(ctx);
                        if (!type_def.HasDefaultPublicConstructor())
                        {
                            ctx.ErrorManager.AddErrorTranslation(ErrorCode.TargetFunctionNotFound, this.autoFieldDefaultInit, ErrorCode.NoDefaultConstructor, this);
                        }
                    }
                    else
                    {
                        ctx.AddError(ErrorCode.CannotAutoInitializeCompoundType, this);
                    }
                }

                IEntityInstance this_eval = null;

                if (tn_eval != null)
                    this_eval = tn_eval;
                else if (init_eval != null)
                    this_eval = init_eval;
                else
                    ctx.AddError(ErrorCode.MissingTypeAndValue, this);

                if (this_eval == null)
                    this_eval = EntityInstance.Joker;

                this.DataTransfer(ctx, ref initValue, this_eval);
                this.Evaluation = this_eval;

                if ((this.IsField() && this.Modifier.HasStatic) || this.isGlobalVariable())
                {
                    if (this.Modifier.HasReassignable)
                        ctx.AddError(ErrorCode.GlobalReassignableVariable, this);
                    if (!this.Evaluation.IsImmutableType(ctx))
                        ctx.AddError(ErrorCode.GlobalMutableVariable, this);
                }

                InitValue?.ValidateValueExpression(ctx);

                if ((!this.EnclosingScope<TemplateDefinition>().IsFunction() || this.Modifier.HasStatic)
                    && this.Evaluation.Enumerate().Any(it => ctx.Env.IsReferenceOfType(it)))
                    ctx.AddError(ErrorCode.PersistentReferenceVariable, this);

                if (this.Evaluation.Enumerate()
                    .Where(it => !ctx.Env.IsPointerOfType(it) && !ctx.Env.IsReferenceOfType(it))
                    .Any(it => it.TargetType.Modifier.HasHeapOnly))
                    ctx.AddError(ErrorCode.HeapTypeOnStack, this);
            }
        }

        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
