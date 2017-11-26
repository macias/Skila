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

        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        public override IEnumerable<INode> OwnedNodes => this.TemplateArguments.Select(it => it.Cast<INode>())
            .Concat(this.Prefix).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Prefix);

        public static NameReference Root => new NameReference(false, null, NameFactory.RootNamespace,
            Enumerable.Empty<INameReference>(), isRoot: true);

        public bool IsDereferenced { get; set; }

        public ExpressionReadMode ReadMode => ExpressionReadMode.ReadRequired;

        public bool IsSink => this.Arity == 0 && this.Prefix == null && this.Name == sink;

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
            {
                if (this.DebugId.Id == 2679 || this.DebugId.Id==2677)
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
                            this.Prefix.IsDereferenced = true;
                    }
                }
                else
                {
                    if (this.IsRoot)
                    {
                        this.Binding.Set(new[] { EntityInstance.Create(ctx, ctx.Env.Root,
                            Enumerable.Empty<INameReference>(),this.OverrideMutability) });
                    }
                    else if (this.Prefix == null)
                    {
                        IEnumerable<IEntity> entities;

                        if (this.Name == NameFactory.SelfFunctionName)
                            entities = new[] { this.EnclosingScope<FunctionDefinition>() };
                        else if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet(this, out IEntity entity))
                        {
                            FunctionDefinition local_function = this.EnclosingScope<FunctionDefinition>();
                            FunctionDefinition entity_function = entity.EnclosingScope<FunctionDefinition>();

                            if (local_function != entity_function)
                            {
                                entity = local_function.LambdaTrap.HijackEscapingReference(entity as VariableDeclaration);
                            }

                            entities = new[] { entity };
                        }
                        else
                        {
                            entities = Enumerable.Empty<IEntity>();
                            foreach (IEntityScope scope in this.EnclosingScopesToRoot().WhereType<IEntityScope>())
                            {
                                entities = scope.FindEntities(this);
                                if (entities.Any())
                                {
                                    break;
                                }
                            }
                        }

                        this.Binding.Set(entities
                            .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments, this.OverrideMutability)));
                    }
                    else
                    {
                        if (this.DebugId.Id == 1734)
                        {
                            ;
                        }

                        // referencing static member?
                        if (this.Prefix is NameReference prefix_ref && prefix_ref.Binding.Match.Target.IsType())
                        {
                            TypeDefinition target_type = prefix_ref.Binding.Match.TargetType;
                            this.Binding.Set(target_type.FindEntities(this)
                                .Where(it => it.Modifier.HasStatic)
                                .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments, this.OverrideMutability)));
                        }
                        else
                        {
                            if (this.DebugId.Id == 2723)
                            {
                                ;
                            }

                            bool dereferenced = false;
                            TemplateDefinition prefix_target = tryDereference(ctx, this.Prefix.Evaluation.Aggregate, ref dereferenced)
                                .TargetTemplate;
                            IEnumerable<IEntity> entities = prefix_target.FindEntities(this)
                                .Where(it => !ctx.Env.Options.StaticMemberOnlyThroughTypeName || !it.Modifier.HasStatic);

                            this.Binding.Set(entities
                                .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments, this.OverrideMutability)));
                            if (this.Prefix.DebugId.Id == 2572)
                            {
                                ;
                            }
                            if (dereferenced)
                                this.Prefix.IsDereferenced = true;
                        }
                    }

                    if (this.Binding.Match.IsJoker && !this.IsSink)
                        ctx.ErrorManager.AddError(ErrorCode.ReferenceNotFound, this);
                    else if (this.Binding.Match.Target is FunctionDefinition func
                        && this.EnclosingScope<FunctionDefinition>() == func
                        && this.Name != NameFactory.SelfFunctionName)
                        ctx.ErrorManager.AddError(ErrorCode.NamedRecursiveReference, this);
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

                this.Evaluation = new EvaluationInfo(eval,aggregate);
            }
        }

        public void Validate(ComputationContext ctx)
        {
            if (this.DebugId.Id == 2773)
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

            if (this.Binding.Match.Target.DebugId.Id == 489)
            {
                ;
            }
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

            // check those names which targets variables and functions, among entities it is sufficient to check if it is an expression
            foreach (IEntity entity in this.Binding.Matches.Select(it => it.Target).WhereType<IExpression>())
            {
                if (entity.OwnerType() == null)
                    continue;

                TemplateDefinition template = this.EnclosingScope<TemplateDefinition>();
                if (!entity.Modifier.HasStatic
                    && ((this.Prefix != null && !this.Prefix.IsValue())
                        || (this.Prefix == null && template.Modifier.HasStatic)))
                    ctx.AddError(ErrorCode.InstanceMemberAccessInStaticContext, this);
            }

            if (ctx.ValAssignTracker != null &&
                !ctx.ValAssignTracker.TryCanRead(this, out VariableDeclaration decl)
                && (this.Owner as Assignment)?.Lhs != this)
            {
                ctx.AddError(ErrorCode.VariableNotInitialized, this, decl);
            }
        }
        private EntityInstance tryDereference(ComputationContext ctx, EntityInstance entityInstance, ref bool dereferenced)
        {
            if (!ctx.Env.Dereferenced(entityInstance,out IEntityInstance __eval,out bool via_pointer))
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

    }
}