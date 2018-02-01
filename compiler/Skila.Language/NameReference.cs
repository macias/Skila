﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class NameReference : Node, ITemplateName, IExpression, INameReference
    {
        private const string sink = "_";

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
            return new NameReference(MutabilityFlag.ConstAsSource, prefix, name, arguments, isRoot: false);
        }
        public static NameReference Create(MutabilityFlag overrideMutability, string name, params INameReference[] arguments)
        {
            return Create(overrideMutability, null, name, arguments);
        }
        public static NameReference Create(MutabilityFlag overrideMutability, IExpression prefix, string name, params INameReference[] arguments)
        {
            return new NameReference(overrideMutability, prefix, name, arguments, isRoot: false);
        }
        public static NameReference Create(IExpression prefix, string name, IEnumerable<INameReference> arguments,
            EntityInstance target)
        {
            var result = new NameReference(MutabilityFlag.ConstAsSource, prefix, name, arguments, isRoot: false);
            if (target != null)
                result.Binding.Set(new[] { target });
            return result;
        }

        public static NameReference CreateBaseInitReference()
        {
            return NameReference.Create(NameFactory.BaseVariableName, NameFactory.InitConstructorName);
        }

        public bool IsBaseInitReference => this.HasBasePrefix && this.Name == NameFactory.InitConstructorName;

        public bool HasBasePrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.BaseVariableName;
        public bool HasThisPrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.ThisVariableName;


        public static NameReference Sink()
        {
            return Create(sink);
        }


        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue && this.isRead != value) throw new Exception("Internal error"); this.isRead = value; } }

        bool INameReference.IsBindingComputed => this.Binding.IsComputed;

        public MutabilityFlag OverrideMutability { get; }
        public bool IsRoot { get; }
        public IExpression Prefix { get; private set; }
        public string Name { get; }
        public IReadOnlyList<INameReference> TemplateArguments { get; }
        public Binding Binding { get; }
        public int Arity => this.TemplateArguments.Count;

        public bool IsComputed { get; private set; }
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        public override IEnumerable<INode> OwnedNodes => this.TemplateArguments.Select(it => it.Cast<INode>())
            .Concat(this.Prefix).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Prefix);

        public bool IsSurfed { get; set; }

        public static NameReference Root => new NameReference(MutabilityFlag.ConstAsSource, null, NameFactory.RootNamespace,
            Enumerable.Empty<INameReference>(), isRoot: true);

        public bool IsDereferencing { get; set; }
        public bool IsDereferenced { get; set; }

        public bool IsSuperReference => this.Name == NameFactory.SuperFunctionName;

        public ExpressionReadMode ReadMode => ExpressionReadMode.ReadRequired;

        public bool IsSink => this.Arity == 0 && this.Prefix == null && this.Name == sink;

        private bool isPropertyIndexerCallReference => this.Owner is FunctionCall call && call.Callee == this && call.IsIndexer;

        private NameReference(
            MutabilityFlag overrideMutability,
            IExpression prefix,
            string name,
            IEnumerable<INameReference> templateArguments,
            bool isRoot)
            : base()
        {
            if (this.DebugId.Id == 7677)
            {
                ;
            }
            this.OverrideMutability = overrideMutability;
            this.IsRoot = isRoot;
            this.Prefix = prefix;
            this.Name = name;
            this.TemplateArguments = (templateArguments ?? Enumerable.Empty<INameReference>()).StoreReadOnlyList();
            this.Binding = new Binding();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }

        public override string ToString()
        {
            string args = "";
            if (TemplateArguments.Any())
                args = "<" + TemplateArguments.Select(it => it.ToString()).Join(",") + ">";
            return new[] { this.Prefix?.ToString(), Name + args }.Where(it => it != null).Join(".");
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
                compute(ctx);

            this.IsComputed = true;
        }

        public void Surf(ComputationContext ctx)
        {
            compute(ctx);
        }

        private void compute(ComputationContext ctx)
        {
            if (this.DebugId.Id == 7636)
            {
                ;
            }

            handleBinding(ctx);

            IEntityInstance eval;
            EntityInstance aggregate;
            computeEval(ctx, out eval, out aggregate);

            if (this.Prefix != null)
            {
                eval = eval.TranslateThrough(this.Prefix.Evaluation.Components);
                aggregate = aggregate.TranslateThrough(this.Prefix.Evaluation.Aggregate);
            }

            this.Evaluation = new EvaluationInfo(eval, aggregate);
        }

        private void computeEval(ComputationContext ctx, out IEntityInstance eval, out EntityInstance aggregate)
        {
            EntityInstance instance = this.Binding.Match;

            if (instance.Target.IsType() || instance.Target.IsNamespace())
            {
                eval = instance;
                aggregate = instance;
            }
            else
            {
                eval = instance.Evaluated(ctx);
                aggregate = instance.Aggregate;
            }
        }

        private void handleBinding(ComputationContext ctx)
        {
            if (this.Binding.IsComputed)
            {
                if (this.Prefix != null)
                {
                    bool dereferenced = false;
                    this.Prefix.Evaluation.Components.EnumerateAll().ForEach(it => tryDereference(ctx, it, ref dereferenced));
                    if (this.Prefix.DebugId.Id == 2572)
                    {
                        ;
                    }
                    if (dereferenced)
                    {
                        this.Prefix.IsDereferenced = true;
                        this.IsDereferencing = true;
                    }
                }
            }
            else
            {
                ErrorCode errorCode = ErrorCode.ReferenceNotFound;
                IEnumerable<EntityInstance> entities = computeBinding(ctx, ref errorCode);

                this.Binding.Set(entities
                    .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments, this.OverrideMutability)));

                if (this.Binding.Match.IsJoker && !this.IsSink
                    // avoid cascade od errors, if prefix failed there is no point in reporting another error
                    && (this.Prefix == null || !this.Prefix.Evaluation.Components.IsJoker))
                {
                    ctx.ErrorManager.AddError(errorCode, this);
                }
            }
        }

        private IEnumerable<EntityInstance> computeBinding(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            if (this.DebugId.Id == 1244)
            {
                ;
            }

            if (this.IsRoot)
            {
                return new[] { ctx.Env.Root.InstanceOf };
            }
            else if (this.Prefix == null)
            {
                if (this.Name == NameFactory.SelfFunctionName)
                    return new[] { this.EnclosingScope<FunctionDefinition>().InstanceOf };
                else if (this.Name == NameFactory.ItTypeName)
                {
                    TypeDefinition enclosing_type = this.EnclosingScope<TypeDefinition>();
                    return new[] { enclosing_type.InstanceOf };
                }
                else if (this.Name == NameFactory.BaseVariableName)
                {
                    TypeDefinition curr_type = this.EnclosingScope<TypeDefinition>();
                    return new[] { curr_type.Inheritance.GetTypeImplementationParent() };
                }
                else if (this.IsSuperReference)
                {
                    FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                    func = func.TryGetSuperFunction(ctx);
                    if (func == null)
                        return Enumerable.Empty<EntityInstance>();
                    else
                        return new[] { func.InstanceOf };
                }
                else if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet(this, out IEntity entity))
                {
                    FunctionDefinition local_function = this.EnclosingScope<FunctionDefinition>();
                    FunctionDefinition entity_function = entity.EnclosingScope<FunctionDefinition>();

                    if (local_function != entity_function
                        // we often share nodes, like parameters in setter/getter, so this check is needed to exclude
                        // such "legal" cases
                        && local_function.EnclosingScopesToRoot().Contains(entity_function))
                    {
                        entity = local_function.LambdaTrap.HijackEscapingReference(entity as VariableDeclaration);
                    }

                    return new[] { entity.InstanceOf };
                }
                else
                {
                    IEnumerable<EntityInstance> entities = Enumerable.Empty<EntityInstance>();
                    foreach (IEntityScope scope in this.EnclosingScopesToRoot().WhereType<IEntityScope>())
                    {
                        entities = scope.FindEntities(this, EntityFindMode.ScopeLimited);
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
                                        entities = filterTargetEntities(entities, it => !(it.Target is IMember) || it.Target.Modifier.HasStatic);
                                    }
                                    else
                                    {
                                        notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;
                                        entities = filterTargetEntities(entities, it =>
                                             !ctx.Env.Options.StaticMemberOnlyThroughTypeName
                                             || !it.Target.Modifier.HasStatic);
                                    }
                                }

                            }

                            break;
                        }
                    }

                    return entities;
                }
            }
            else
            {
                if (this.DebugId.Id == 27425)
                {
                    ;
                }

                EntityFindMode find_mode = this.isPropertyIndexerCallReference
                    ? EntityFindMode.AvailableIndexersOnly : EntityFindMode.WithCurrentProperty;

                // referencing static member?
                if (this.Prefix is NameReference prefix_ref
                    // todo: make it nice, currently refering to base look like static reference
                    && prefix_ref.Name != NameFactory.BaseVariableName
                    && prefix_ref.Binding.Match.Target.IsTypeContainer())
                {
                    EntityInstance target_instance = prefix_ref.Binding.Match;
                    IEnumerable<EntityInstance> entities = target_instance.FindEntities(ctx, this, find_mode);

                    if (entities.Any())
                        notFoundErrorCode = ErrorCode.InstanceMemberAccessInStaticContext;

                    if (target_instance.Target is TypeDefinition typedef)
                        entities = filterTargetEntities(entities, it => it.Target.Modifier.HasStatic);

                    return entities;
                }
                else
                {
                    if (this.DebugId.Id == 27425)
                    {
                        ;
                    }

                    bool dereferenced = false;
                    EntityInstance prefix_instance = tryDereference(ctx, this.Prefix.Evaluation.Aggregate, ref dereferenced);
                    IEnumerable<EntityInstance> entities = prefix_instance.FindEntities(ctx, this, find_mode);
                    {
                        IEntityInstance prefix_eval = this.Prefix.Evaluation.Components;
                        // we need to get value evaluation to perform translation
                        ctx.Env.Dereference(prefix_eval, out prefix_eval);
                        entities = entities.Select(it => it.TranslateThrough(prefix_eval));
                    }

                    if (entities.Any())
                        notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;

                    entities = filterTargetEntities(entities, it => !ctx.Env.Options.StaticMemberOnlyThroughTypeName
                        || !it.Target.Modifier.HasStatic);

                    if (this.Prefix.DebugId.Id == 2572)
                    {
                        ;
                    }
                    if (dereferenced)
                    {
                        this.Prefix.IsDereferenced = true;
                        this.IsDereferencing = true;
                    }

                    return entities;
                }
            }
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
            if (this.DebugId.Id == 7677)
            {
                ;
            }

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
                else if (mismatch == ConstraintMatch.ConstViolation)
                    ctx.AddError(ErrorCode.ViolatedConstConstraint, this);
                else if (mismatch == ConstraintMatch.InheritsViolation)
                    ctx.AddError(ErrorCode.ViolatedInheritsConstraint, this);
                else if (mismatch == ConstraintMatch.MissingFunction)
                    ctx.AddError(ErrorCode.ViolatedHasFunctionConstraint, this);
                else if (mismatch != ConstraintMatch.Yes)
                    throw new Exception("Internal error");
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

            trySetTargetUsage(this.Binding.Match);

            if (this.IsSuperReference && this.EnclosingScope<FunctionDefinition>().Modifier.HasUnchainBase)
                ctx.AddError(ErrorCode.SuperCallWithUnchainedBase, this);

            {
                IEntity binding_target = this.Binding.Match.Target;
                if (binding_target.Modifier.HasPrivate)
                {
                    // if the access to entity is private and it is overriden entity it means we have Non-Virtual Interface pattern
                    // as we should forbid access to such entity
                    // this condition checks only if disallow opening access from private to protected/public during derivation
                    if (binding_target.Modifier.HasOverride
                        // this is classic check if we are targeting types in current scope
                        || !(this).EnclosingScopesToRoot().Contains(binding_target.EnclosingScope<TypeContainerDefinition>()))
                    {
                        ctx.AddError(ErrorCode.AccessForbidden, this);
                    }
                }
            }

            if (this.HasBasePrefix
                && (!(this.Binding.Match.Target is global::Skila.Language.Entities.FunctionDefinition target_func)
                // exclusion for constructors because it might be legal or not, but nevertheless
                // both cases are handled elsewhere
                || !target_func.IsAnyConstructor())
                && !ctx.Env.Options.ReferencingBase)
                ctx.ErrorManager.AddError(ErrorCode.CrossReferencingBaseMember, this);

            {
                if (this.Prefix == null && !this.HasThisPrefix
                    && this.Binding.Match.Target is IMember member && !member.Modifier.HasStatic)
                {
                    FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                    TypeDefinition enclosing_type = this.EnclosingScope<TypeDefinition>();
                    if (enclosing_type != null && enclosing_type.AvailableEntities.Select(it => it.Target).Contains(member)
                         // in lambdas do not require fully qualified name because user sees it a function
                         // not a method inside closure type
                         && (enclosing_func == null || !enclosing_func.IsLambdaInvoker))
                        ctx.AddError(ErrorCode.MissingThisPrefix, this);
                }
            }

            if (this.Binding.Match.Target is FunctionParameter param && param.UsageMode == ExpressionReadMode.CannotBeRead)
                ctx.AddError(ErrorCode.CannotReadExpression, this);

            // todo: after reshaping escape analysis and associated reference types extend this to types as well
            if (this.Binding.Match.Target is FunctionDefinition)
                foreach (INameReference arg in this.TemplateArguments)
                    if (ctx.Env.IsReferenceOfType(arg.Evaluation.Components))
                        ctx.AddError(ErrorCode.ReferenceAsTypeArgument, arg);
        }

        public void ValidateTypeNameVariance(ComputationContext ctx, VarianceMode typeNamePosition)
        {
            // Programming in Scala, 2nd ed, p. 399 (all errors are mine)

            TypeDefinition typedef = this.Binding.Match.TargetType;

            if (typedef.IsTemplateParameter)
            {
                TemplateParameter param = typedef.TemplateParameter;
                TemplateDefinition template = param.EnclosingScope<TemplateDefinition>();
                if (this.EnclosingScopesToRoot().Contains(template))
                {
                    bool covariant_in_immutable = param.Variance == VarianceMode.Out
                        && (template.IsFunction() || template.CastType().InstanceOf.MutabilityOfType(ctx) == MutabilityFlag.ConstAsSource);

                    // don't report errors for covariant types which are used in immutable template types
                    if (!covariant_in_immutable &&
                        typeNamePosition.PositionCollides(param.Variance))
                        ctx.AddError(ErrorCode.VarianceForbiddenPosition, this, param);
                }
            }
            else
                for (int i = 0; i < typedef.Name.Parameters.Count; ++i)
                {
                    this.TemplateArguments[i].Cast<NameReference>().ValidateTypeNameVariance(ctx,
                        typeNamePosition.Flipped(typedef.Name.Parameters[i].Variance));
                }

        }

        private EntityInstance tryDereference(ComputationContext ctx, EntityInstance entityInstance, ref bool dereferenced)
        {
            if (!ctx.Env.Dereferenced(entityInstance, out IEntityInstance __eval))
                return entityInstance;

            dereferenced = true;

            // todo: this is incorrect, just a temporary shortcut
            return __eval.Cast<EntityInstance>();
        }

        public NameReference Recreate(IEnumerable<INameReference> arguments, EntityInstance target)
        {
            IExpression this_prefix = this.Prefix;
            this_prefix?.DetachFrom(this);
            this.TemplateArguments.ForEach(it => it.DetachFrom(this));

            var result = new NameReference(this.OverrideMutability, this_prefix, this.Name, arguments, this.IsRoot);
            result.Binding.Set(new[] { target });
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

            TypeDefinition target_type = callTarget.CastFunction().ContainingType();
            FunctionDefinition current_function = this.EnclosingScope<FunctionDefinition>();
            if (target_type != null && target_type == current_function.ContainingType())
            {
                NameReference implicit_this = current_function.GetThisNameReference();
                return implicit_this;
            }

            return null;
        }
        public bool IsLValue(ComputationContext ctx)
        {
            if (this.DebugId.Id == 7255)
            {
                ;
            }

            if (this.IsSink)
            {
                return true;
            }

            if (!(this.Binding.Match.Target is IEntityVariable))
                return false;

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
    }
}