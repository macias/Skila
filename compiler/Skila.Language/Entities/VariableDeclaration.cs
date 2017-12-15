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
    public sealed class VariableDeclaration : Expression, IEntityVariable, ILambdaTransfer, ILocalBindable, IMember
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
        public bool IsMemberUsed { get; private set; }

        public override IEnumerable<INode> OwnedNodes => new INode[] { TypeName, InitValue, Modifier }
            .Where(it => it != null)
            .Concat(closures);
        public override ExecutionFlow Flow => ExecutionFlow.CreatePath(InitValue);
        public EntityModifier Modifier { get; private set; }

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

            this.instanceOf = new Lazy<EntityInstance>(() => EntityInstance.RAW_CreateUnregistered(this, EntityInstanceSignature.None));

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

        public override bool AttachTo(INode owner)
        {
            if (!base.AttachTo(owner))
                return false;

            if (owner is TypeContainerDefinition && !this.Modifier.IsAccessSet)
                this.SetModifier(this.Modifier | EntityModifier.Private);

            return true;
        }

        private void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments, bool overrideMutability)
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
            if (this.DebugId.Id == 3191)
            {
                ;
            }

            if (this.InitValue.IsUndef())
                return null;

            // we have to give target for name, because this could be property field, and it is in scope
            // of a property not enclosed type, so from constructor such field is invisible
            NameReference field_reference = this.Name.CreateNameReference(
                this.Modifier.HasStatic ? NameFactory.ItTypeReference() : NameFactory.ThisReference(), this.InstanceOf);

            if (this.InitValue == null)
            {
                // we need to save it for later to change the errors, user does not see this call, but she/he
                // sees the field
                this.autoFieldDefaultInit = FunctionCall.Constructor(NameReference.Create(field_reference,
                    NameFactory.InitConstructorName));
                return this.autoFieldDefaultInit;
            }
            else if (!this.InitValue.IsUndef())
            {
                this.initValue.DetachFrom(this);

                FunctionCall init;

                // if the init value is constructor call, there is no point in creating around it
                // another constructor call. Instead get the init step and reuse it, this time
                // with given field directly, for example
                // x = Foo()
                // translatates to
                // x = (__this__ = alloc Foo ; __this__.init() ; __this__)
                // so we rip off the init step and replace the object, which results in
                // x.init()
                if (this.initValue is Block block && block.Mode == Block.Purpose.Initialization)
                {
                    init = block.InitializationStep;
                    init.DetachFrom(block);

                    init.Name.ReplacePrefix(field_reference);
                }
                else
                {
                    init = FunctionCall.Constructor(NameReference.Create(field_reference, NameFactory.InitConstructorName),
                        FunctionArgument.Create(this.initValue));
                }

                this.initValue = null;
                return init;
            }
            else
                throw new InvalidOperationException();
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 2636)
                {
                    ;
                }

                this.TrapClosure(ctx, ref this.initValue);

                IEntityInstance init_eval = InitValue?.Evaluation?.Components;

                IEntityInstance tn_eval = this.TypeName?.Evaluation?.Components;
                if (tn_eval != null
                    && ((InitValue == null && this.isGlobalVariable())
                        || (this.IsField() && this.autoFieldDefaultInit != null)))
                {
                    if (this.TypeName.TryGetSingleType(out NameReference type_name, out EntityInstance type_instance))
                    {
                        TypeDefinition type_def = type_instance.TargetType;
                        if (!type_def.HasDefaultConstructor())
                        {
                            ctx.ErrorManager.AddErrorTranslation(ErrorCode.TargetFunctionNotFound, this.autoFieldDefaultInit,
                            ErrorCode.NoDefaultConstructor, this);
                        }
                    }
                    else
                    {
                        ctx.AddError(ErrorCode.CannotAutoInitializeCompoundType, this);
                    }
                }

                IEntityInstance this_eval = null;
                EntityInstance this_aggregate = null;

                if (tn_eval != null)
                {
                    this_eval = tn_eval;
                    this_aggregate = this.TypeName.Evaluation.Aggregate;
                }
                else if (init_eval != null)
                {
                    this_eval = init_eval;
                    this_aggregate = this.InitValue.Evaluation.Aggregate;
                }
                else
                    ctx.AddError(ErrorCode.MissingTypeAndValue, this);

                if (this_eval == null)
                {
                    this_eval = EntityInstance.Joker;
                    this_aggregate = EntityInstance.Joker;
                }

                this.DataTransfer(ctx, ref initValue, this_eval);
                this.Evaluation = new EvaluationInfo(this_eval, this_aggregate);

                if ((this.IsField() && this.Modifier.HasStatic) || this.isGlobalVariable())
                {
                    if (this.Modifier.HasReassignable)
                        ctx.AddError(ErrorCode.GlobalReassignableVariable, this);
                    if (!this.Evaluation.Components.IsImmutableType(ctx))
                        ctx.AddError(ErrorCode.GlobalMutableVariable, this);
                }

                InitValue?.ValidateValueExpression(ctx);

                if ((!this.EnclosingScope<TemplateDefinition>().IsFunction() || this.Modifier.HasStatic)
                    && this.Evaluation.Components.Enumerate().Any(it => ctx.Env.IsReferenceOfType(it)))
                    ctx.AddError(ErrorCode.PersistentReferenceVariable, this);

                if (this.Evaluation.Components.Enumerate()
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

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            if (!ctx.Env.Options.TypelessVariablesDuringTests && this.Owner is TypeContainerDefinition && this.TypeName == null)
                ctx.AddError(ErrorCode.MissingTypeName, this);

            if (!ctx.Env.Options.GlobalVariables && this.Owner is Namespace)
                ctx.AddError(ErrorCode.GlobalVariable, this);
        }

        public void SetIsMemberUsed()
        {
            if (this.DebugId.Id == 2965)
            {
                ;
            }
            this.IsMemberUsed = true;
        }

    }
}
