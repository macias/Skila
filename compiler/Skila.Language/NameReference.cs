using System;
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

        public static NameReference CreateThised(string part)
        {
            return NameReference.Create(NameFactory.ThisVariableName, part);
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
            return new NameReference(false, prefix, name, arguments, isRoot: false);
        }
        public static NameReference Create(bool overrideMutability, IExpression prefix, string name, params INameReference[] arguments)
        {
            return new NameReference(overrideMutability, prefix, name, arguments, isRoot: false);
        }
        public static NameReference Create(IExpression prefix, string name, IEnumerable<INameReference> arguments,
            EntityInstance target)
        {
            var result = new NameReference(false, prefix, name, arguments, isRoot: false);
            if (target != null)
                result.Binding.Set(new[] { target });
            return result;
        }

        public static NameReference CreateBaseInitReference()
        {
            return NameReference.Create(NameFactory.BaseVariableName, NameFactory.InitConstructorName);
        }

        public bool IsBaseInitReference => this.hasBasePrefix && this.Name == NameFactory.InitConstructorName;

        private bool hasBasePrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.BaseVariableName;
        private bool hasThisPrefix => this.Prefix is NameReference name_ref && name_ref.Name == NameFactory.ThisVariableName;


        public static NameReference Sink()
        {
            return Create(sink);
        }


        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue && this.isRead != value) throw new Exception("Internal error"); this.isRead = value; } }

        bool INameReference.IsBindingComputed => this.Binding.IsComputed;

        public bool OverrideMutability { get; }
        public bool IsRoot { get; }
        public IExpression Prefix { get; private set; }
        public string Name { get; }
        public IReadOnlyCollection<INameReference> TemplateArguments { get; }
        public Binding Binding { get; }
        public int Arity => this.TemplateArguments.Count;

        public bool IsComputed { get; private set; }
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        public override IEnumerable<INode> OwnedNodes => this.TemplateArguments.Select(it => it.Cast<INode>())
            .Concat(this.Prefix).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Prefix);

        public bool IsSurfed { get; set; }
        public IEnumerable<ISurfable> Surfables => this.OwnedNodes.WhereType<ISurfable>();

        public static NameReference Root => new NameReference(false, null, NameFactory.RootNamespace,
            Enumerable.Empty<INameReference>(), isRoot: true);

        public bool IsDereferencing { get; set; }
        public bool IsDereferenced { get; set; }

        public bool IsSuperReference => this.Name == NameFactory.SuperFunctionName;

        public ExpressionReadMode ReadMode => ExpressionReadMode.ReadRequired;

        public bool IsSink => this.Arity == 0 && this.Prefix == null && this.Name == sink;

        private bool isPropertyIndexerCallReference => this.Owner is FunctionCall call && call.Callee == this && call.IsIndexer;

        private NameReference(
            bool overrideMutability,
            IExpression prefix,
            string name,
            IEnumerable<INameReference> templateArguments,
            bool isRoot)
            : base()
        {
            this.OverrideMutability = overrideMutability;
            this.IsRoot = isRoot;
            this.Prefix = prefix;
            this.Name = name;
            this.TemplateArguments = (templateArguments ?? Enumerable.Empty<INameReference>()).StoreReadOnly();
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
            if (this.DebugId.Id == 3277)
            {
                ;
            }

            if (this.Binding.IsComputed)
            {
                if (this.Prefix != null)
                {
                    bool dereferenced = false;
                    this.Prefix.Evaluation.Components.Enumerate().ForEach(it => tryDereference(ctx, it, ref dereferenced));
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
                IEnumerable<IEntity> entities = computeBinding(ctx, ref errorCode);

                this.Binding.Set(entities
                    .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments, this.OverrideMutability)));

                if (this.Binding.Match.IsJoker && !this.IsSink
                    // avoid cascade od errors, if prefix failed there is no point in reporting another error
                    && (this.Prefix == null || !this.Prefix.Evaluation.Components.IsJoker))
                {
                    ctx.ErrorManager.AddError(errorCode, this);
                }
            }

            EntityInstance instance = this.Binding.Match;
            IEntityInstance eval;
            EntityInstance aggregate;
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

            if (this.Prefix != null)
            {
                eval = eval.TranslateThrough(this.Prefix.Evaluation.Components);
                aggregate = aggregate.TranslateThrough(this.Prefix.Evaluation.Aggregate);
            }

            this.Evaluation = new EvaluationInfo(eval, aggregate);
        }

        private IEnumerable<IEntity> computeBinding(ComputationContext ctx,
            // we pass error code because in some case we will be able to give more precise reason for error
            ref ErrorCode notFoundErrorCode)
        {
            if (this.DebugId.Id == 2956)
            {
                ;
            }
            if (this.IsRoot)
            {
                return new[] { ctx.Env.Root };
            }
            else if (this.Prefix == null)
            {
                if (this.Name == NameFactory.SelfFunctionName)
                    return new[] { this.EnclosingScope<FunctionDefinition>() };
                else if (this.Name == NameFactory.ItTypeName)
                    return new[] { this.EnclosingScope<TypeDefinition>() };
                else if (this.Name == NameFactory.BaseVariableName)
                {
                    TypeDefinition curr_type = this.EnclosingScope<TypeDefinition>();
                    return new[] { curr_type.Inheritance.GetTypeImplementationParent().Target };
                }
                else if (this.IsSuperReference)
                {
                    FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                    func = func.TryGetSuperFunction(ctx);
                    if (func == null)
                        return Enumerable.Empty<IEntity>();
                    else
                        return new[] { func };
                }
                else if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet(this, out IEntity entity))
                {
                    FunctionDefinition local_function = this.EnclosingScope<FunctionDefinition>();
                    FunctionDefinition entity_function = entity.EnclosingScope<FunctionDefinition>();

                    if (local_function != entity_function)
                    {
                        entity = local_function.LambdaTrap.HijackEscapingReference(entity as VariableDeclaration);
                    }

                    return new[] { entity };
                }
                else
                {
                    IEnumerable<IEntity> entities = Enumerable.Empty<IEntity>();
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
                                        entities = filterTargetEntities(entities,it => !(it is IMember) || it.Modifier.HasStatic);
                                    }
                                    else
                                    {
                                        notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;
                                        entities = filterTargetEntities(entities,it => 
//                                            !(it is IMember)
                                            //||
                                            !ctx.Env.Options.StaticMemberOnlyThroughTypeName
                                            || !it.Modifier.HasStatic);
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
                if (this.DebugId.Id == 1734)
                {
                    ;
                }

                EntityFindMode find_mode = this.isPropertyIndexerCallReference
                    ? EntityFindMode.AvailableIndexersOnly : EntityFindMode.WithCurrentProperty;

                // referencing static member?
                if (this.Prefix is NameReference prefix_ref
                    // todo: make it nice, currently refering to base look like static reference
                    && prefix_ref.Name != NameFactory.BaseVariableName
                    && prefix_ref.Binding.Match.Target.IsType())
                {
                    TypeDefinition target_type = prefix_ref.Binding.Match.TargetType;
                    IEnumerable<IEntity> entities = target_type.FindEntities(this, find_mode);

                    if (entities.Any())
                        notFoundErrorCode = ErrorCode.InstanceMemberAccessInStaticContext;

                    entities = filterTargetEntities(entities,it => it.Modifier.HasStatic);

                    return entities;
                }
                else
                {
                    if (this.DebugId.Id == 3277)
                    {
                        ;
                    }

                    bool dereferenced = false;
                    TemplateDefinition prefix_target = tryDereference(ctx, this.Prefix.Evaluation.Aggregate, ref dereferenced)
                        .TargetTemplate;
                    IEnumerable<IEntity> entities = prefix_target.FindEntities(this, find_mode);

                    if (entities.Any())
                        notFoundErrorCode = ErrorCode.StaticMemberAccessInInstanceContext;

                    entities = filterTargetEntities(entities,it => !ctx.Env.Options.StaticMemberOnlyThroughTypeName || !it.Modifier.HasStatic);

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
        private IEnumerable<IEntity> filterTargetEntities(IEnumerable<IEntity> entities, Func<IEntity, bool> pred)
        {
            IEntity entity = entities.FirstOrDefault();
            entities = entities.Where(pred);
            if (entity != null && !entities.Any())
                trySetTargetUsage(entity);

            return entities;
        }

        private void trySetTargetUsage(IEntity target)
        {
            if ((this.Owner as Assignment)?.Lhs != this)
            {
                FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                // zero constructor is built automatically so any usage inside does not count
                if ((enclosing_func == null || !enclosing_func.IsZeroConstructor())
                    && target is IMember member)
                    member.SetIsMemberUsed();
            }
        }
        public void Validate(ComputationContext ctx)
        {
            if (this.DebugId.Id == 3044)
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

            trySetTargetUsage(this.Binding.Match.Target);
            /*    if ((this.Owner as Assignment)?.Lhs != this)
                {
                    FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                    // zero constructor is built automatically so any usage inside does not count
                    if ((enclosing_func == null || !enclosing_func.IsZeroConstructor())
                        && this.Binding.Match.Target is IMember member)
                        member.SetIsMemberUsed();
                }*/

            if (this.IsSuperReference && this.EnclosingScope<FunctionDefinition>().Modifier.HasUnchainBase)
                ctx.AddError(ErrorCode.SuperCallWithUnchainedBase, this);

            {
                IEntity binding_target = this.Binding.Match.Target;
                if (binding_target.Modifier.HasPrivate)
                {
                    // if the access to entity is private and it is overriden entity it means we have Non-Virtual Interface pattern
                    // as we should forbid access to such entity
                    // this condition checks only if disallow opening access from private to protected/public during derivation
                    if (binding_target.Modifier.HasRefines
                        // this is classic check if we are targeting types in current scope
                        || !(this).EnclosingScopesToRoot().Contains(binding_target.EnclosingScope<TypeContainerDefinition>()))
                    {
                        ctx.AddError(ErrorCode.AccessForbidden, this);
                    }
                }
            }

            if (this.hasBasePrefix
                && (!(this.Binding.Match.Target is global::Skila.Language.Entities.FunctionDefinition target_func)
                // exclusion for constructors because it might be legal or not, but nevertheless
                // both cases are handled elsewhere
                || !target_func.IsConstructor())
                && !ctx.Env.Options.ReferencingBase)
                ctx.ErrorManager.AddError(ErrorCode.CrossReferencingBaseMember, this);

            {
                if (this.Prefix == null && !this.hasThisPrefix
                    && this.Binding.Match.Target is IMember member && !member.Modifier.HasStatic)
                {
                    FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();
                    TypeDefinition enclosing_type = this.EnclosingScope<TypeDefinition>();
                    if (enclosing_type != null && enclosing_type.AvailableEntities.Contains(member)
                         // in lambdas do not require fully qualified name because user sees it a function
                         // not a method inside closure type
                         && (enclosing_func == null || !enclosing_func.IsLambdaInvoker))
                        ctx.AddError(ErrorCode.MissingThisPrefix, this);
                }
            }

            if (this.Binding.Match.Target is FunctionParameter param && param.UsageMode == ExpressionReadMode.CannotBeRead)
                ctx.AddError(ErrorCode.CannotReadExpression, this);
        }

        private EntityInstance tryDereference(ComputationContext ctx, EntityInstance entityInstance, ref bool dereferenced)
        {
            if (!ctx.Env.Dereferenced(entityInstance, out IEntityInstance __eval, out bool via_pointer))
                return entityInstance;

            dereferenced = true;

            // todo: this is incorrect, just a temporary shortcut
            return __eval.Cast<EntityInstance>();
        }

        public NameReference Recreate(IEnumerable<INameReference> arguments)
        {
            var result = new NameReference(this.OverrideMutability, this.Prefix, this.Name, arguments, this.IsRoot);
            result.AttachTo(this);
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
                return prefix;

            if (!callTarget.IsFunction())
                throw new Exception("Internal error (if it is callable why is not wrapped into closure already)");

            TypeDefinition target_type = callTarget.CastFunction().OwnerType();
            FunctionDefinition current_function = this.EnclosingScope<FunctionDefinition>();
            if (target_type != null && target_type == current_function.OwnerType())
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