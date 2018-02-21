using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        public EntityInstance InstanceOf => this.instancesCache.InstanceOf;
        private readonly EntityInstanceCache instancesCache;
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

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(null, MutabilityFlag.ConstAsSource,
                translation: TemplateTranslation.Create(this)));

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
            if (this.DebugId.Id == 123498)
            {
                ;
            }
            if (!base.AttachTo(owner))
                return false;

            if (owner is TypeContainerDefinition && !this.Modifier.IsAccessSet)
                this.SetModifier(this.Modifier | EntityModifier.Private);

            if (owner is IEntity entity_owner && entity_owner.Modifier.HasStatic && !this.Modifier.HasStatic)
                this.SetModifier(this.Modifier | EntityModifier.Static);

            return true;
        }

        private void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public EntityInstance GetInstance(IEnumerable<IEntityInstance> arguments, MutabilityFlag overrideMutability,
            TemplateTranslation translation)
        {
            return this.instancesCache.GetInstance(arguments, overrideMutability, translation);
        }

        public override bool IsReadingValueOfNode(IExpression node)
        {
            return node == this.InitValue;
        }

        private bool isGlobalVariable()
        {
            return this.Scope.IsNamespace();
        }

        public IExpression DetachFieldInitialization()
        {
            if (this.DebugId.Id == 297)
            {
                ;
            }

            if (this.InitValue.IsUndef())
                return null;

            if (this.InitValue == null)
            {
                // we need to save it for later to change the errors, user does not see this call, but she/he
                // sees the field
                this.autoFieldDefaultInit = FunctionCall.Constructor(NameReference.Create(fieldReference(),
                    NameFactory.InitConstructorName));
                return this.autoFieldDefaultInit;
            }
            else if (!this.InitValue.IsUndef())
            {
                this.initValue.DetachFrom(this);

                IExpression init;

                // if the init value is constructor call, there is no point in creating around it
                // another constructor call. Instead get the init step and reuse it, this time
                // with given field directly, for example
                // x = Foo()
                // translates to
                // x = (__this__ = alloc Foo ; __this__.init() ; __this__)
                // so we rip off the init step and replace the object, which results in
                // x.init()
                if (this.initValue is Block block && block.Mode == Block.Purpose.Initialization 
                    // do not use this optimization for heap objects!
                    && !block.HeapInitialization)
                {
                    FunctionCall cons_call = block.InitializationStep;
                    cons_call.DetachFrom(block);

                    cons_call.Name.ReplacePrefix(fieldReference());
                    init = cons_call;
                }
                else
                {
                    init = Assignment.CreateStatement(fieldReference(), this.initValue);
                }

                this.initValue = null;
                return init;
            }
            else
                throw new InvalidOperationException();
        }

        public FunctionCall CreateFieldInitCall(IExpression initExpr)
        {
            return FunctionCall.Constructor(NameReference.Create(fieldReference(), NameFactory.InitConstructorName),
                                    FunctionArgument.Create(initExpr));
        }

        private NameReference fieldReference()
        {
            return this.Name.CreateNameReference(
                            this.Modifier.HasStatic ? NameFactory.ItTypeReference() : NameFactory.ThisReference(),
            // we have to give target for name, because this could be property field, and it is in scope
            // of a property not enclosed type, so from constructor such field is invisible
                            this.InstanceOf);
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation != null)
                return;

            if (this.DebugId.Id == 291)
            {
                ;
            }

            this.TrapClosure(ctx, ref this.initValue);

            IEntityInstance init_eval = InitValue?.Evaluation?.Components;

            IEntityInstance tn_eval = this.TypeName?.Evaluation?.Components;
            if (tn_eval != null
                && ((InitValue == null && this.isGlobalVariable())
                    || (this.IsTypeContained() && this.autoFieldDefaultInit != null)))
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
            else
            {
                MutabilityFlag mutability = this_eval.MutabilityOfType(ctx);
                if (mutability == MutabilityFlag.DualConstMutable)
                {
                    this_eval = this_eval.Rebuild(ctx, MutabilityFlag.ForceMutable);
                    this_aggregate = this_aggregate.Rebuild(ctx, MutabilityFlag.ForceMutable).Cast<EntityInstance>();
                }
            }

            this.DataTransfer(ctx, ref initValue, this_eval);
            this.Evaluation = new EvaluationInfo(this_eval, this_aggregate);

            if ((this.IsTypeContained() && this.Modifier.HasStatic) || this.isGlobalVariable())
            {
                if (this.Modifier.HasReassignable)
                    ctx.AddError(ErrorCode.GlobalReassignableVariable, this);
                if (this.Evaluation.Components.MutabilityOfType(ctx) != MutabilityFlag.ConstAsSource)
                    ctx.AddError(ErrorCode.GlobalMutableVariable, this);
            }
        }

        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }

        private bool validateStorage(ComputationContext ctx, TypeDefinition ownerType)
        {
            // todo: improve this to detect types containing arrays of values of itself (for example)
            if (ownerType == null || this.Modifier.HasStatic)
                return true;

            TypeDefinition eval_type = this.Evaluation.Components.Target().CastType();
            if (ownerType == eval_type)
                return false;

            foreach (VariableDeclaration field in eval_type.AllNestedFields)
            {
                if (!field.validateStorage(ctx, ownerType))
                    return false;
            }

            return true;
        }
        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            InitValue?.ValidateValueExpression(ctx);

            // only for constructor call allow to have non-reference
            if (!(this.InitValue is Alloc))
                this.ValidateReferenceAssociatedReference(ctx);

            if (!validateStorage(ctx, this.ContainingType()))
                ctx.AddError(ErrorCode.NestedValueOfItself, this);

            if (!ctx.Env.Options.AllowEmptyFieldTypeNames && this.Owner is TypeContainerDefinition && this.TypeName == null)
                ctx.AddError(ErrorCode.MissingTypeName, this);

            if (!ctx.Env.Options.GlobalVariables && this.Owner is Namespace)
                ctx.AddError(ErrorCode.GlobalVariable, this);

            {
                TypeDefinition containing_type = this.ContainingType();
                if (containing_type != null)
                {
                    if (containing_type.IsTrait || containing_type.IsInterface || containing_type.IsProtocol)
                        ctx.AddError(ErrorCode.FieldInNonImplementationType, this);

                    if (this.Name.Name != NameFactory.PropertyAutoField)
                    {
                        // consider Foo<T> with field f, type T
                        // can I pass instance of Foo<U> (U extends T) somewhere?
                        // sure, as long the field cannot be reassigned (otherwise the receiver could reset it to T, while callee would expect U)
                        this.TypeName.Cast<NameReference>().ValidateTypeNameVariance(ctx,
                            this.Modifier.HasReassignable ? VarianceMode.None : VarianceMode.Out);
                    }
                }
            }

            {
                TemplateDefinition enclosing_template = this.EnclosingScope<TemplateDefinition>();
                if ((!enclosing_template.IsFunction() || this.Modifier.HasStatic)
                    && !enclosing_template.Modifier.HasAssociatedReference
                    && ctx.Env.IsReferenceOfType(Evaluation.Components))
                {
                    ctx.AddError(ErrorCode.PersistentReferenceVariable, this);
                }
            }

            if (this.Evaluation.Components.EnumerateAll()
                .Where(it => !ctx.Env.IsPointerOfType(it) && !ctx.Env.IsReferenceOfType(it))
                .Any(it => it.TargetType.Modifier.HasHeapOnly))
            {
                ctx.AddError(ErrorCode.HeapTypeOnStack, this);
            }


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
