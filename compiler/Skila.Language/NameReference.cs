﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Tools;
using Skila.Language.Printout;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReference : OwnedNode, ITemplateName, IExpression, INameReference
    {
        public static NameReference CreateThised(params string[] parts)
        {
            return NameReference.Create(new[] { NameFactory.ThisVariableName }.Concat(parts).ToArray());
        }
        public static NameReference Create(params string[] parts)
        {
            if (parts.Length == 0)
                throw new ArgumentException();
            else if (parts.Length == 1)
                return NameReference.Create(parts.Single());
            else
                return NameReference.Create(NameReference.Create(parts.SkipTail(1).ToArray()), parts.Last());
        }
        public static NameReference Create(string name, params INameReference[] arguments)
        {
            return NameReference.Create(null, name, arguments);
        }
        public static NameReference Create(IExpression prefix, string name, params INameReference[] arguments)
        {
            return Create(prefix, name, ExpressionReadMode.ReadRequired, arguments);
        }
        public static NameReference Create(IExpression prefix, LifetimeScope lifetimeScope, string name, params INameReference[] arguments)
        {
            return Create(prefix, lifetimeScope, name, ExpressionReadMode.ReadRequired, arguments);
        }
        public static NameReference Create(IExpression prefix, string name)
        {
            return Create(prefix, name, new INameReference[] { });
        }
        public static NameReference Create(IExpression prefix, BrowseMode browse, string name, params INameReference[] arguments)
        {
            return Create(prefix, browse, name, ExpressionReadMode.ReadRequired, arguments);
        }
        public static NameReference Create(IExpression prefix, string name, ExpressionReadMode readMode,
            params INameReference[] arguments)
        {
            return new NameReference(TypeMutability.None, prefix, BrowseMode.None, null, name,
                arguments.Select(it => new TemplateArgument(it)), readMode, isRoot: false);
        }
        public static NameReference Create(IExpression prefix, LifetimeScope lifetimeScope, string name, ExpressionReadMode readMode,
            params INameReference[] arguments)
        {
            return new NameReference(TypeMutability.None, prefix, BrowseMode.None, lifetimeScope, name,
                arguments.Select(it => new TemplateArgument(it)), readMode, isRoot: false);
        }
        public static NameReference Create(IExpression prefix, BrowseMode browse, string name, ExpressionReadMode readMode,
            params INameReference[] arguments)
        {
            return new NameReference(TypeMutability.None, prefix, browse, null, name,
                arguments.Select(it => new TemplateArgument(it)), readMode, isRoot: false);
        }
        public static NameReference Create(TypeMutability overrideMutability, string name, params INameReference[] arguments)
        {
            return Create(overrideMutability, null, name, arguments);
        }
        public static NameReference Create(TypeMutability overrideMutability, IExpression prefix, string name,
            params INameReference[] arguments)
        {
            return new NameReference(overrideMutability, prefix, BrowseMode.None, null,
                name, arguments.Select(it => new TemplateArgument(it)),
                ExpressionReadMode.ReadRequired, isRoot: false);
        }
        public static NameReference Create(TypeMutability overrideMutability, IExpression prefix, LifetimeScope lifetimeScope, string name,
            params INameReference[] arguments)
        {
            return new NameReference(overrideMutability, prefix, BrowseMode.None, lifetimeScope,
                name, arguments.Select(it => new TemplateArgument(it)),
                ExpressionReadMode.ReadRequired, isRoot: false);
        }
        public static NameReference Create(TypeMutability overrideMutability, IExpression prefix, string name)
        {
            return new NameReference(overrideMutability, prefix, BrowseMode.None, null,
                name, Enumerable.Empty<TemplateArgument>(),
                ExpressionReadMode.ReadRequired, isRoot: false);
        }
        public static NameReference Create(TypeMutability overrideMutability, IExpression prefix, string name,
            IEnumerable<INameReference> arguments, EntityInstance target, bool isLocal)
        {
            var result = new NameReference(overrideMutability, prefix, BrowseMode.None, null, name,
                arguments?.Select(it => new TemplateArgument(it)),
                ExpressionReadMode.ReadRequired, isRoot: false);
            if (target != null)
                result.Binding.Set(new[] { new BindingMatch(target, isLocal) });
            return result;
        }
        public static NameReference Create(IExpression prefix, string name, IEnumerable<INameReference> arguments,
            EntityInstance target, bool isLocal)
        {
            return Create(TypeMutability.None, prefix, name, arguments, target, isLocal);
        }

        public static NameReference CreateBaseInitReference()
        {
            return NameReference.Create(NameFactory.BaseVariableName, NameFactory.InitConstructorName);
        }

        public bool IsBaseInitReference => this.HasBasePrefix && this.Name == NameFactory.InitConstructorName;

        public bool HasBasePrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.BaseVariableName;
        public bool HasThisPrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.ThisVariableName;

        public bool IsSelfTypeName => this.Name == NameFactory.SelfTypeTypeName;

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue && this.isRead != value) throw new Exception("Internal error"); this.isRead = value; } }

        bool INameReference.IsBindingComputed => this.Binding.IsComputed;

        public TypeMutability OverrideMutability { get; }
        public bool IsRoot { get; }
        public IExpression Prefix { get; private set; }
        public string Name { get; }
        public IReadOnlyList<TemplateArgument> TemplateArguments { get; }
        public Binding Binding { get; }
        public int Arity => this.TemplateArguments.Count;

        public bool IsComputed { get; private set; }
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        public override IEnumerable<INode> ChildrenNodes => this.TemplateArguments.Select(it => it.Cast<INode>())
            .Concat(this.Prefix).Where(it => it != null);
        private readonly Later<ExecutionFlow> flow;
        public ExecutionFlow Flow => this.flow.Value;

        public bool IsSurfed { get; set; }

        public static NameReference Root => new NameReference(TypeMutability.None, null, BrowseMode.None, null,
            NameFactory.RootNamespace,
            Enumerable.Empty<TemplateArgument>(), ExpressionReadMode.ReadRequired, isRoot: true);

        public int DereferencingCount { get; set; }
        public int DereferencedCount_LEGACY { get; set; }

        public bool IsSuperReference => this.Name == NameFactory.SuperFunctionName;

        private readonly BrowseMode browse;
        private readonly LifetimeScope? lifetimeScope;

        public ExpressionReadMode ReadMode { get; }

        public bool IsSink => this.Arity == 0 && this.Prefix == null && this.Name == NameFactory.Sink;

        private bool isPropertyIndexerCallReference => this.Owner is FunctionCall call && call.Callee == this && call.IsIndexer;

        private NameReference(
            TypeMutability overrideMutability,
            IExpression prefix,
            BrowseMode browse,
            LifetimeScope? lifetimeScope,
            string name,
            IEnumerable<TemplateArgument> templateArguments,
            ExpressionReadMode readMode,
            bool isRoot)
            : base()
        {
            this.browse = browse;
            this.lifetimeScope = lifetimeScope;
            this.ReadMode = readMode;
            this.OverrideMutability = overrideMutability;
            this.IsRoot = isRoot;
            this.Prefix = prefix;
            this.Name = name;
            this.TemplateArguments = (templateArguments ?? Enumerable.Empty<TemplateArgument>()).StoreReadOnlyList();
            this.Binding = new Binding();

            this.attachPostConstructor();

            this.flow = Later.Create(() => ExecutionFlow.CreatePath(Prefix));
        }

        public override string ToString()
        {
            return Printout().ToString();
        }

        public ICode Printout()
        {
            var args = new CodeSpan();
            if (TemplateArguments.Any())
            {
                args = new CodeSpan(TemplateArguments.Select(it => it.Printout()).ToArray());
                args.Prepend("<").Append(">");
            }

            var code = new CodeSpan(Name).Append(args);
            if (this.Prefix != null)
                code.Prepend(".").Prepend(this.Prefix);

            code.Prepend(this.OverrideMutability.StringPrefix());

            return code;
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                compute(ctx);

            this.IsComputed = true;
        }

        public void Surf(ComputationContext ctx)
        {
            this.ChildrenNodes.WhereType<ISurfable>().ForEach(it => it.Surfed(ctx));

            compute(ctx);
        }

        private void compute(ComputationContext ctx)
        {
            handleBinding(ctx);

            IEntityInstance eval;
            EntityInstance aggregate;

            computeEval(ctx, out eval, out aggregate);

            if (this.Prefix != null)
            {
                eval = eval.TranslateThrough(this.Prefix.Evaluation.Components);
                aggregate = aggregate.TranslateThrough(this.Prefix.Evaluation.Aggregate);
            }

            this.Evaluation = EvaluationInfo.Create(eval, aggregate);
        }

        private void computeEval(ComputationContext ctx, out IEntityInstance eval, out EntityInstance aggregate)
        {
            if (this.DebugId == (7, 11169))
            {
                ;
            }
            EntityInstance instance = this.Binding.Match.Instance;

            if (instance.Target.IsTypeContainer())
            {
                Lifetime lifetime;
                {
                    if (instance.Target is TypeDefinition type_def)
                    {
                        if (type_def == ctx.Env.ReferenceType)
                            lifetime = Lifetime.Create(this, this.lifetimeScope ?? LifetimeScope.Local);
                        else if (type_def == ctx.Env.PointerType)
                            lifetime = Lifetime.Create(this, this.lifetimeScope ?? LifetimeScope.Global);
                        else
                            lifetime = Lifetime.Create(this, this.lifetimeScope ?? LifetimeScope.Local);

                    }
                    else
                        lifetime = Lifetime.Create(this);
                }

                instance = instance.Build(this.OverrideMutability, lifetime);
                eval = instance;
                aggregate = instance;
            }
            else
            {
                eval = instance.Evaluated(ctx);
                aggregate = instance.Aggregate;

                if (this.Prefix != null) // adjust lifetime according to prefix
                {
                    Lifetime lifetime;

                    // in case of reference we are accessing data through reference, so no change
                    if (ctx.Env.IsReferenceOfType(this.Prefix.Evaluation.Aggregate))
                        lifetime = null;
                    // this is most restrictive approach, if we access no-pointer field of heap object
                    // clip the lifetime to local, this way no one can be create permanent reference to such field
                    // (it is technically possible but would require adding more logic to GC)
                    else if (ctx.Env.IsPointerOfType(this.Prefix.Evaluation.Aggregate) && !ctx.Env.IsPointerOfType(aggregate))
                        lifetime = Lifetime.Create(this, LifetimeScope.Local);
                    else
                        lifetime = this.Prefix.Evaluation.Aggregate.Lifetime;

                    if (lifetime != null)
                    {
                        eval = eval.Rebuild(ctx, lifetime, deep: false);
                        aggregate = aggregate.Build(lifetime);
                    }
                }

                if (this.Prefix != null)
                {
                    TypeMutability prefix_mutability = this.Prefix.Evaluation.Components.MutabilityOfType(ctx);
                    if (prefix_mutability == TypeMutability.ForceConst)
                    {
                        eval = eval.Rebuild(ctx, TypeMutability.ForceConst);
                        aggregate = aggregate.Rebuild(ctx, TypeMutability.ForceConst);
                    }
                }
            }
        }

        private void handleBinding(ComputationContext ctx)
        {
            if (this.Binding.IsComputed)
                return;

            ErrorCode errorCode = ErrorCode.ReferenceNotFound;
            IEnumerable<BindingMatch> entities = computeBinding(ctx, ref errorCode);

            this.Binding.Set(entities);

            if (this.Binding.Match.Instance.IsJoker && !this.IsSink
                // avoid cascade od errors, if prefix failed there is no point in reporting another error
                && (this.Prefix == null || !this.Prefix.Evaluation.Components.IsJoker))
            {
                ctx.ErrorManager.AddError(errorCode, this);
            }
        }

        private IEnumerable<BindingMatch> computeBinding(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            IEnumerable<BindingMatch> entities = rawComputeBinding(ctx, ref notFoundErrorCode).StoreReadOnly();
            IEnumerable<BindingMatch> aliases = entities.Where(it => it.Instance.Target is Alias alias);
            if (aliases.Any())
                entities = aliases.SelectMany(it =>
                {
                    var alias = it.Instance.Target.Cast<Alias>();
                    alias.SetIsMemberUsed();
                    NameReference name_reference = alias.Replacement.Cast<NameReference>();
                    name_reference.Surfed(ctx);
                    return name_reference.Binding.Matches;
                });

            if (notFoundErrorCode == ErrorCode.StaticMemberAccessInInstanceContext)
                entities = filterCrossAccessEntities(ctx, entities.Select(it => it.Instance))
                    .Select(it => new BindingMatch(it, isLocal: false));

            return entities;
        }

        private IEnumerable<BindingMatch> rawComputeBinding(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            if (this.IsRoot)
                return new[] { new BindingMatch(ctx.Env.Root.InstanceOf, isLocal: false) };
            else if (this.Prefix == null)
                return rawComputeBindingNoPrefix(ctx, ref notFoundErrorCode);
            else
                return rawComputeBindingWithPrefix(ctx, ref notFoundErrorCode);
        }

        private IEnumerable<BindingMatch> rawComputeBindingWithPrefix(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            if (this.DebugId == (7, 11169))
            {
                ;
            }
            EntityInstance prefix_instance = tryDereference(ctx, this.Prefix.Evaluation.Aggregate);

            if (this.Name == NameFactory.ItTypeName || this.IsSelfTypeName)
            {
                return new[] { new BindingMatch(prefix_instance, isLocal: false) };
            }

            // referencing static member?
            if (this.Prefix is NameReference prefix_ref
                // todo: make it nice, currently refering to base look like static reference
                && prefix_ref.Name != NameFactory.BaseVariableName
                && prefix_ref.Binding.Match.Instance.Target.IsTypeContainer())
            {
                EntityInstance target_instance = prefix_ref.Binding.Match.Instance;
                IEnumerable<EntityInstance> entities = target_instance.FindEntities(ctx, this);

                if (entities.Any())
                    notFoundErrorCode = ErrorCode.InstanceMemberAccessInStaticContext;

                if (target_instance.Target is TypeDefinition typedef)
                    entities = filterTargetEntities(entities, it => it.Target.Modifier.HasStatic);

                return entities
                    .Select(it => new BindingMatch(it.Build(this.TemplateArguments, this.OverrideMutability), isLocal: false));
            }
            else
            {
                IEnumerable<EntityInstance> entities = prefix_instance.FindEntities(ctx, this);

                if (!entities.Any())
                    entities = prefix_instance.FindExtensions(ctx, this);

                {
                    IEntityInstance prefix_eval = this.Prefix.Evaluation.Components;
                    // we need to get value evaluation to perform translation
                    ctx.Env.Dereference(prefix_eval, out prefix_eval);
                    entities = entities.Select(it => it.TranslateThrough(prefix_eval));
                }

                if (entities.Any())
                    notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;

                return entities.Select(it => new BindingMatch(it.Build(this.TemplateArguments, this.OverrideMutability), isLocal: false));
            }
        }

        private IEnumerable<BindingMatch> rawComputeBindingNoPrefix(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            if (this.Name == NameFactory.RecurFunctionName)
            {
                return new[] { new BindingMatch(this.EnclosingScope<FunctionDefinition>().InstanceOf, isLocal: false) };
            }
            else if (this.Name == NameFactory.ItTypeName || this.IsSelfTypeName)
            {
                TypeDefinition enclosing_type = this.EnclosingScope<TypeDefinition>();
                return new[] { new BindingMatch(enclosing_type.InstanceOf.Build(this.OverrideMutability), isLocal: false) };
            }
            else if (this.Name == NameFactory.BaseVariableName)
            {
                TypeDefinition curr_type = this.EnclosingScope<TypeDefinition>();
                return new[] { new BindingMatch(curr_type.Inheritance.GetTypeImplementationParent(), isLocal: false) };
            }
            else if (this.IsSuperReference)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                func = func.TryGetSuperFunction(ctx);
                if (func == null)
                    return Enumerable.Empty<BindingMatch>();
                else
                    return new[] { new BindingMatch(func.InstanceOf, isLocal: false) };
            }
            else if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet(this, out IEntity entity))
            {
                FunctionDefinition local_function = this.EnclosingScope<FunctionDefinition>();
                FunctionDefinition entity_function = entity.EnclosingScope<FunctionDefinition>();

                bool is_local = true;
                if (local_function != entity_function
                    // we often share nodes, like parameters in setter/getter, so this check is needed to exclude
                    // such "legal" cases
                    && local_function.EnclosingScopesToRoot().Contains(entity_function))
                {
                    entity = local_function.LambdaTrap.HijackEscapingReference(entity as VariableDeclaration);
                    is_local = false;
                }

                return new[] { new BindingMatch(entity.InstanceOf, isLocal: is_local) };
            }
            else
            {
                IEnumerable<EntityInstance> entities = Enumerable.Empty<EntityInstance>();
                foreach (IEntityScope scope in this.EnclosingScopesToRoot().WhereType<IEntityScope>())
                {
                    entities = scope.InstanceOf.FindEntities(ctx, this);
                    if (entities.Any())
                    {
                        TypeDefinition enclosed_type = this.EnclosingScope<TypeDefinition>();
                        if (enclosed_type == scope)
                        {
                            FunctionDefinition enclosing_function = this.EnclosingScope<FunctionDefinition>();
                            if (enclosing_function != null)
                            {
                                if (enclosing_function.Modifier.HasStatic)
                                {
                                    notFoundErrorCode = ErrorCode.InstanceMemberAccessInStaticContext;
                                    entities = filterTargetEntities(entities, it => !(it.Target is IMember)
                                        || it.Target.Modifier.HasStatic);
                                }
                                else
                                {
                                    notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;
                                }
                            }

                        }

                        break;
                    }
                }

                return entities
                    .Select(it => new BindingMatch(it.Build(this.TemplateArguments, this.OverrideMutability), isLocal: false));
            }
        }

        private IEnumerable<EntityInstance> filterCrossAccessEntities(ComputationContext ctx, IEnumerable<EntityInstance> entities)
        {
            if (this.browse.HasFlag(BrowseMode.InstanceToStatic) || !ctx.Env.Options.StaticMemberOnlyThroughTypeName || this.Prefix == null)
                return entities;
            else
                return filterTargetEntities(entities, it => !it.Target.Modifier.HasStatic
                                        || ((it.Target is FunctionDefinition func) && func.IsExtension));
        }

        // this function tries to minimize number of errors -- in case we cannot resolve a name
        // because of the filter, we set potential target as used, otherwise we would get both errors (not found + not used)
        private IEnumerable<EntityInstance> filterTargetEntities(IEnumerable<EntityInstance> entities, Func<EntityInstance, bool> pred)
        {
            EntityInstance entity = entities.FirstOrDefault();
            entities = entities.Where(pred);
            if (entity != null && !entities.Any())
                trySetTargetUsage(entity);

            return entities;
        }

        private void trySetTargetUsage(EntityInstance entityInstance)
        {
            if ((this.Owner as Assignment)?.Lhs != this)
            {
                FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                // zero constructor is built automatically so any usage inside does not count
                if ((enclosing_func == null || !enclosing_func.IsZeroConstructor())
                    && entityInstance.Target is IMember member)
                    member.SetIsMemberUsed();
            }
        }
        public void Validate(ComputationContext ctx)
        {
            {
                ConstraintMatch mismatch = ConstraintMatch.Yes;
                this.Binding.Filter(instance =>
                {
                    ConstraintMatch m = TypeMatcher.ArgumentsMatchConstraintsOf(ctx, instance);
                    if (m == ConstraintMatch.Yes)
                        return true;
                    else
                    {
                        mismatch = m;
                        return false;
                    }
                });

                if (!this.Binding.Matches.Any())
                {
                    if (mismatch == ConstraintMatch.BaseViolation)
                        ctx.AddError(ErrorCode.ViolatedBaseConstraint, this);
                    else if (mismatch == ConstraintMatch.MutabilityViolation)
                        ctx.AddError(ErrorCode.ViolatedMutabilityConstraint, this);
                    else if (mismatch == ConstraintMatch.AssignabilityViolation)
                        ctx.AddError(ErrorCode.ViolatedMutabilityConstraint, this);
                    else if (mismatch == ConstraintMatch.InheritsViolation)
                        ctx.AddError(ErrorCode.ViolatedInheritsConstraint, this);
                    else if (mismatch == ConstraintMatch.MissingFunction)
                        ctx.AddError(ErrorCode.ViolatedHasFunctionConstraint, this);
                    // todo: added in a rush, polish this scenario
                    else if (mismatch == ConstraintMatch.UndefinedTemplateArguments)
                        ctx.AddError(ErrorCode.UndefinedTemplateArguments, this);
                    else if (mismatch != ConstraintMatch.Yes)
                        throw new Exception("Internal error");
                }
            }

            /*  {
                  // check those names which target type members (fields, methods, properties)
                  foreach (IMember member in this.Binding.Matches.Select(it => it.Target).WhereType<IMember>())
                  {
                      if (member.OwnerType() == null)
                          continue;

                      TemplateDefinition template = this.EnclosingScope<TemplateDefinition>();
                      if (!member.Modifier.HasStatic
                          && ((this.Prefix != null && !this.Prefix.IsValue(ctx.Env.Options))
                              || (this.Prefix == null && template.Modifier.HasStatic)))
                          ctx.AddError(ErrorCode.InstanceMemberAccessInStaticContext, this);
                  }
              }*/


            if ((this.Owner as Assignment)?.Lhs != this)
            {
                if (ctx.ValAssignTracker != null
                    && !ctx.ValAssignTracker.TryCanRead(this, out VariableDeclaration decl))
                {
                    ctx.AddError(ErrorCode.VariableNotInitialized, this, decl);
                }
            }


            trySetTargetUsage(this.Binding.Match.Instance);

            FunctionDefinition enclosing_function = this.EnclosingScope<FunctionDefinition>();

            if (this.Name == NameFactory.SelfTypeTypeName)
            {
                // allow Self parameters in constructor only (at least for now)
                FunctionParameter enclosing_param = this.EnclosingNode<FunctionParameter>();

                if (enclosing_function == null || (enclosing_param != null && !enclosing_function.IsAnyConstructor()))
                    ctx.AddError(ErrorCode.SelfTypeOutsideConstructor, this);
            }

            if (this.IsSuperReference && enclosing_function.Modifier.HasUnchainBase)
                ctx.AddError(ErrorCode.SuperCallWithUnchainedBase, this);

            {
                IEntity binding_target = this.Binding.Match.Instance.Target;
                if (binding_target.Modifier.HasPrivate)
                {
                    // if the access to entity is private and it is overriden entity it means we have Non-Virtual Interface pattern
                    // as we should forbid access to such entity
                    // this condition checks only if disallow opening access from private to protected/public during derivation

                    // the only exception is calling base function 
                    // (we don't break into the logic of NVI, just making the chain of overrides) 
                    if (!this.IsSuperReference && (binding_target.Modifier.HasOverride
                        // testing whether we are targeting current type
                        || !(this).EnclosingScopesToRoot().Contains(binding_target.EnclosingScope<TypeContainerDefinition>())))
                    {
                        if (!this.browse.HasFlag(BrowseMode.Decompose))
                            ctx.AddError(ErrorCode.AccessForbidden, this);
                    }
                    else if (binding_target is IRestrictedMember member && member.AccessGrants.Any())
                    {
                        FunctionDefinition curr_func = this.EnclosingScope<FunctionDefinition>();
                        if (!curr_func.IsAnyConstructor() && !member.AccessGrants.Select(it => it.Binding).Contains(curr_func))
                            ctx.AddError(ErrorCode.AccessForbidden, this);
                    }
                }
            }

            if (this.HasBasePrefix
                && (!(this.Binding.Match.Instance.Target is global::Skila.Language.Entities.FunctionDefinition target_func)
                // exclusion for constructors because it might be legal or not, but nevertheless
                // both cases are handled elsewhere
                || !target_func.IsAnyConstructor())
                && !ctx.Env.Options.ReferencingBase)
            {
                ctx.ErrorManager.AddError(ErrorCode.CrossReferencingBaseMember, this);
            }

            {
                if (this.Prefix == null && !this.HasThisPrefix
                    && this.Binding.Match.Instance.Target is IMember member && !member.Modifier.HasStatic)
                {
                    FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                    TypeDefinition enclosing_type = this.EnclosingScope<TypeDefinition>();
                    if (enclosing_type != null && enclosing_type.AvailableEntities.Select(it => it.Target).Contains(member)
                         // in lambdas do not require fully qualified name because user sees it a function
                         // not a method inside closure type
                         // and outside functions allow direct name reference (without it/this; it should be OK)
                         && enclosing_func != null && !enclosing_func.IsLambdaInvoker)
                    {
                        ctx.AddError(ErrorCode.MissingThisPrefix, this);
                    }
                }
            }

            if (this.Binding.Match.Instance.Target is FunctionParameter param && param.UsageMode == ExpressionReadMode.CannotBeRead)
                ctx.AddError(ErrorCode.CannotReadExpression, this);

            // todo: after reshaping escape analysis and associated reference types extend this to types as well
            if (this.Binding.Match.Instance.Target is FunctionDefinition)
                foreach (TemplateArgument arg in this.TemplateArguments)
                    if (ctx.Env.IsReferenceOfType(arg.TypeName.Evaluation.Components))
                        ctx.AddError(ErrorCode.ReferenceAsTypeArgument, arg);
        }

        private static EntityInstance tryDereference(ComputationContext ctx, EntityInstance entityInstance)
        {
            int dereferenced = ctx.Env.Dereference(entityInstance, out IEntityInstance __eval);
            if (dereferenced == 0)
                return entityInstance;

            // todo: this is incorrect, just a temporary shortcut
            return __eval.Cast<EntityInstance>();
        }

        public NameReference Recreate(IEnumerable<TemplateArgument> arguments, EntityInstance target, bool isLocal)
        {
            IExpression this_prefix = this.Prefix;
            this_prefix?.DetachFrom(this);
            this.TemplateArguments.ForEach(it => it.DetachFrom(this));

            var result = new NameReference(this.OverrideMutability, this_prefix, this.browse, this.lifetimeScope, this.Name,
                arguments, this.ReadMode, this.IsRoot);
            result.Binding.Set(new[] { new BindingMatch(target, isLocal) });
            return result;
        }
        public NameReference CreateWith(IEnumerable<TemplateArgument> arguments, EntityInstance target, bool isLocal)
        {
            var result = new NameReference(this.OverrideMutability, this.Prefix, this.browse, this.lifetimeScope, this.Name,
                arguments, this.ReadMode, this.IsRoot);
            result.Binding.Set(new[] { new BindingMatch(target, isLocal) });
            return result;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }

        public IExpression GetContext(IEntity callTarget)
        {
            IExpression prefix = this.Prefix;
            if (prefix != null)
            {
                return prefix;
            }

            if (!callTarget.IsFunction())
                throw new Exception("Internal error (if it is callable why is not wrapped into closure already)");

            // it does NOT compute context in case of recall on lambda/closure, this is not by design
            // we noticed it is not really need (at least so far) and fixing it here would need more changes
            // than single check when building binaries/executing code, 
            // all it takes is check if we have recall and if yes, preserve `this`

            TypeDefinition target_type = callTarget.CastFunction().ContainingType();
            if (target_type != null)
            {
                FunctionDefinition current_function = this.EnclosingScope<FunctionDefinition>();
                TypeDefinition current_type = current_function.ContainingType();
                if (target_type == current_type)
                {
                    NameReference implicit_this = current_function.GetThisNameReference();
                    return implicit_this;
                }
            }

            return null;
        }
        public bool IsLValue(ComputationContext ctx)
        {

            if (this.IsSink)
            {
                return true;
            }

            if (!(this.Binding.Match.Instance.Target is IEntityVariable))
            {
                return (this.Binding.Match.Instance.Target is TypeContainerDefinition);
            }

            if (this.Prefix != null)
                return this.Prefix.IsLValue(ctx);

            return true;
        }

        public void ReplacePrefix(IExpression prefix)
        {
            this.Prefix.DetachFrom(this);
            this.Prefix = prefix;
            this.Prefix.AttachTo(this);
        }

        public bool IsExactlySame(INameReference other, EntityInstance translationTemplate, bool jokerMatchesAll)
        {
            bool identical = Object.ReferenceEquals(this, other);
            if (!jokerMatchesAll || identical)
                return identical;

            if (this.Evaluation.Aggregate.IsJoker || other.Evaluation.Aggregate.IsJoker)
                return true;

            var other_nameref = other as NameReference;
            if (other_nameref == null)
                return other.IsExactlySame(this, translationTemplate, jokerMatchesAll);

            if (this.OverrideMutability != other_nameref.OverrideMutability)
                return false;

            if (this.Name == NameFactory.SelfTypeTypeName && other_nameref.Name == NameFactory.SelfTypeTypeName)
                return true;

            if (this.TemplateArguments.Count != other_nameref.TemplateArguments.Count)
                return false;

            for (int i = 0; i < this.TemplateArguments.Count; ++i)
            {
                if (!this.TemplateArguments[i].TypeName.IsExactlySame(other_nameref.TemplateArguments[i].TypeName,
                    translationTemplate, jokerMatchesAll))
                    return false;
            }

            IEntityInstance this_trans_eval = this.Evaluation.Components.TranslateThrough(translationTemplate);
            if (!this_trans_eval.HasExactlySameTarget(other.Evaluation.Components, jokerMatchesAll))
                return false;

            return true;
        }

    }
}