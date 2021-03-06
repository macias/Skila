﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class VariableDeclaration : Expression, IEntityVariable, ILambdaTransfer, ILocalBindable, IRestrictedMember
    {
        public static VariableDeclaration CreateStatement(string name, INameReference typeName, IExpression initValue,
            EntityModifier modifier = null)
        {
            return new VariableDeclaration(modifier, ExpressionReadMode.CannotBeRead, name, typeName, initValue);
        }
        public static VariableDeclaration CreateExpression(string name, INameReference typeName, IExpression initValue)
        {
            return new VariableDeclaration(EntityModifier.None, ExpressionReadMode.ReadRequired, name, typeName, initValue);
        }
        public static VariableDeclaration Create(ExpressionReadMode readMode, string name, INameReference typeName,
            IExpression initValue, EntityModifier modifier, IEnumerable<LabelReference> friends)
        {
            return new VariableDeclaration(modifier, readMode, name, typeName, initValue, friends);
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

        public override IEnumerable<INode> ChildrenNodes => new INode[] { TypeName, InitValue, Modifier }
            .Where(it => it != null)
            .Concat(this.AccessGrants)
            .Concat(closures);

        private readonly Later<ExecutionFlow> flow;
        public override ExecutionFlow Flow => this.flow.Value;
        public EntityModifier Modifier { get; private set; }
        public IEnumerable<LabelReference> AccessGrants { get; }

        private VariableDeclaration(EntityModifier modifier, ExpressionReadMode readMode, string name,
            INameReference typeName,
            IExpression initValue,
            IEnumerable<LabelReference> friends = null)
            : base(readMode)
        {
            if (name == null)
                throw new ArgumentNullException();

            this.Modifier = modifier ?? EntityModifier.None;
            this.Name = NameDefinition.Create(name);
            this.TypeName = typeName;
            this.initValue = initValue;
            this.AccessGrants = (friends ?? Enumerable.Empty<LabelReference>()).StoreReadOnly();

            this.instancesCache = new EntityInstanceCache(this, () => GetInstance(TypeMutability.None,
                TemplateTranslation.CreateParameterless(this), Lifetime.Timeless));

            this.closures = new List<TypeDefinition>();

            this.attachPostConstructor();

            this.flow = Later.Create(() => ExecutionFlow.CreatePath(InitValue));
        }
        public override string ToString()
        {
            return this.Printout().ToString();
        }
        public override ICode Printout()
        {
            CodeSpan code = new CodeSpan(this, Name).Prepend(this.Modifier.HasReassignable ? "var " : "let ");
            if (this.TypeName != null)
                code.Append(" of ").Append(this.TypeName);
            if (this.InitValue != null)
            {
                code.Append(" " + (this.ReadMode == ExpressionReadMode.ReadRequired ? "<-" : "=") + " ").Append(this.InitValue);
            }
            if (this.ReadMode == ExpressionReadMode.ReadRequired)
                code.Prepend("(").Append(")");

            return code;
        }

        public override bool AttachTo(IOwnedNode owner)
        {
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

        public EntityInstance GetInstance(TypeMutability overrideMutability,
            TemplateTranslation translation, Lifetime lifetime)
        {
            return this.instancesCache.GetInstance( overrideMutability, translation, lifetime);
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
                if (this.initValue is New block
                    // do not use this optimization for heap objects!
                    && !block.IsHeapInitialization)
                {
                    FunctionCall cons_call = block.InitConstructorCall;
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
                            this.Modifier.HasStatic ? NameFactory.ItNameReference() : NameFactory.ThisReference(),
            // we have to give target for name, because this could be property field, and it is in scope
            // of a property not enclosed type, so from constructor such field is invisible
                            this.InstanceOf, isLocal: false);
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation != null)
                return;

            if (this.DebugId== (18, 381))
            {
                ;
            }
            this.TrapClosure(ctx, ref this.initValue);

            IEntityInstance init_eval = this.InitValue?.Evaluation?.Components;

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
                if (this.InitValue.IsUndef())
                    ctx.AddError(ErrorCode.MissingTypeName, this);

                this_eval = init_eval;
                this_aggregate = this.InitValue.Evaluation.Aggregate;
            }
            else
                ctx.AddError(ErrorCode.MissingTypeAndValue, this);

            if (this_eval == null)
            {
                this_eval = Environment.JokerInstance;
                this_aggregate = Environment.JokerInstance;
            }
            else
            {
                TypeMutability mutability = this_eval.SurfaceMutabilityOfType(ctx);
                if (tn_eval == null)
                {
                    if (this.Modifier.HasMutable)
                    {
                        this_eval = this_eval.Rebuild(ctx, TypeMutability.ForceMutable);
                        this_aggregate = this_aggregate.Rebuild(ctx, TypeMutability.ForceMutable);
                    }
                    else if (mutability == TypeMutability.DualConstMutable)
                    {
                        TypeMutability this_override = TypeMutability.ForceConst;
                        if (!mutability.HasFlag(TypeMutability.Reassignable) && this.Modifier.HasReassignable)
                            this_override |= TypeMutability.Reassignable;
                        this_eval = this_eval.Rebuild(ctx, this_override);
                        this_aggregate = this_aggregate.Rebuild(ctx, TypeMutability.ForceConst);
                    }
                }
                else if (!mutability.HasFlag(ctx.Env.Options.ReassignableTypeMutability())
                    && this.Modifier.Has(ctx.Env.Options.ReassignableModifier()))
                {
                    TypeMutability mutability_override = ctx.Env.Options.ReassignableTypeMutability();
                    if (tn_eval != null && this.TypeName is NameReference name_ref)
                        mutability_override |= name_ref.OverrideMutability;
                    this_eval = this_eval.Rebuild(ctx, mutability_override, deep: false);
                }
            }

            this.Evaluation = new EvaluationInfo(this_eval, this_aggregate);

            this.DataTransfer(ctx, ref initValue, this.Evaluation.Components);
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

            if (ctx.Env.Options.MutabilityMode == MutabilityModeOption.SingleMutability && this.Modifier.HasReassignable)
                throw new ArgumentException("Cannot have both");

            this.ValidateRestrictedMember(ctx);
            InitValue?.ValidateValueExpression(ctx);

            if ((this.IsTypeContained() && this.Modifier.HasStatic) || this.isGlobalVariable())
            {
                if (this.Modifier.HasReassignable)
                    ctx.AddError(ErrorCode.GlobalMutableVariable, this);
                else if (ctx.Env.Options.MutabilityMode != MutabilityModeOption.OnlyAssignability)
                {
                    TypeMutability mutability = this.Evaluation.Components.MutabilityOfType(ctx);
                    if (!mutability.HasFlag(TypeMutability.ForceConst) && !mutability.HasFlag(TypeMutability.ConstAsSource))
                        ctx.AddError(ErrorCode.GlobalMutableVariable, this);
                }
            }

            // only for constructor call allow to have non-reference
            if (!(this.InitValue is Alloc))
                this.ValidateReferenceAssociatedReference(ctx);

            if (!validateStorage(ctx, this.ContainingType()))
                ctx.AddError(ErrorCode.NestedValueOfItself, this);

            if (this.TypeName == null)
            {
                if (!ctx.Env.Options.AllowEmptyFieldTypeNames && this.Owner is TypeContainerDefinition)
                    ctx.AddError(ErrorCode.MissingTypeName, this);
            }

            if (!ctx.Env.Options.GlobalVariables && this.Owner is Namespace)
                ctx.AddError(ErrorCode.GlobalVariable, this);

            {
                TypeDefinition containing_type = this.ContainingType();
                if (containing_type != null)
                {
                    if (containing_type.IsTrait || containing_type.IsInterface || containing_type.IsProtocol)
                        ctx.AddError(ErrorCode.FieldInNonImplementationType, this);

                    if (!this.Modifier.HasAutoGenerated) // auto-property field
                    {
                        // consider Foo<T> with field f, type T
                        // can I pass instance of Foo<U> (U extends T) somewhere?
                        // sure, as long the field cannot be reassigned (otherwise the receiver could reset it to T, while callee would expect U)
                        this.TypeName.ValidateTypeNameVariance(ctx, this.IsReassignable(ctx) ? VarianceMode.None : VarianceMode.Out);
                    }
                }
            }

            if (!ctx.Env.Options.AllowReferenceFields)
            {
                TemplateDefinition enclosing_template = this.EnclosingScope<TemplateDefinition>();
                if ((!enclosing_template.IsFunction() || this.Modifier.HasStatic)
                    && ctx.Env.IsReferenceOfType(Evaluation.Components))
                {
                    if (!enclosing_template.Modifier.HasAssociatedReference)
                    {
                        ctx.AddError(ErrorCode.PersistentReferenceVariable, this);
                    }
                }
            }

            this.TypeName?.ValidateHeapTypeName(ctx);
        }

        public void SetIsMemberUsed()
        {
            this.IsMemberUsed = true;
        }

        public bool IsReassignable(ComputationContext ctx)
        {
            if (this.Modifier.HasReassignable)
                return true;

            if (ctx.Env.Options.MutabilityMode == MutabilityModeOption.SingleMutability)
            {
                TypeMutability mutability = this.Evaluation.Components.SurfaceMutabilityOfType(ctx);
                if (mutability.HasFlag(TypeMutability.ForceMutable))
                    return true;
            }

            return false;
        }
    }
}
