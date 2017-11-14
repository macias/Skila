using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Flow;
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
            return new NameReference(prefix, name, arguments, isRoot: false);
        }
        public static NameReference Create(IExpression prefix, string name, IEnumerable<INameReference> arguments,
            EntityInstance target)
        {
            var result = new NameReference(prefix, name, arguments, isRoot: false);
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

        public bool IsRoot { get; }
        public IExpression Prefix { get; }
        public string Name { get; }
        public IReadOnlyCollection<INameReference> TemplateArguments { get; }
        public Binding Binding { get; }
        public int Arity => this.TemplateArguments.Count;

        public bool IsComputed => this.Evaluation != null;
        public IEntityInstance Evaluation { get; private set; }
        public ValidationData Validation { get; set; }

        public override IEnumerable<INode> OwnedNodes => this.TemplateArguments.Select(it => it.Cast<INode>())
            .Concat(this.Prefix).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Prefix);

        public static NameReference Root => new NameReference(null, NameFactory.RootNamespace,
            Enumerable.Empty<INameReference>(), isRoot: true);

        public bool IsDereferenced { get; set; }

        public ExpressionReadMode ReadMode => ExpressionReadMode.ReadRequired;

        public bool IsSink => this.Arity == 0 && this.Prefix == null && this.Name == sink;

        private NameReference(
            IExpression prefix,
            string name,
            IEnumerable<INameReference> templateArguments,
            bool isRoot)
            : base()
        {
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
                if (this.DebugId.Id == 8809)
                {
                    ;
                }

                if (!this.Binding.IsComputed)
                {
                    this.TemplateArguments.ForEach(it => it.Evaluate(ctx));

                    if (this.IsRoot)
                    {
                        this.Binding.Set(new[] { EntityInstance.Create(ctx,ctx.Env.Root,
                            Enumerable.Empty<INameReference>()) });
                    }
                    else if (this.Prefix == null)
                    {
                        IEntity entity;
                        IEnumerable<IEntity> entities;

                        if (ctx.EvalLocalNames != null && ctx.EvalLocalNames.TryGet<IEntity>(this, out entity))
                            entities = new[] { entity };
                        else
                        {
                            entities = Enumerable.Empty<IEntity>();
                            foreach (IEntityScope scope in this.EnclosingScopesToRoot().WhereType<IEntityScope>())
                            {
                                entities = scope.FindEntities(this);
                                if (entities.Any())
                                    break;
                            }
                        }

                        this.Binding.Set(entities
                            .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments)));
                    }
                    else
                    {
                        this.Prefix.Evaluated(ctx);

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
                                .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments)));
                        }
                        else
                        {
                            if (this.DebugId.Id == 8809)
                            {
                                ;
                            }

                            bool dereferenced = false;
                            this.Binding.Set(this.Prefix.Evaluation
                                .Enumerate()
                                .Select(it => tryDereference(ctx, it, ref dereferenced).TargetTemplate)
                                .SelectMany(it => it.FindEntities(this))
                                .Where(it => !ctx.Env.Options.StaticMemberOnlyThroughTypeName || !it.Modifier.HasStatic)
                                .Select(it => EntityInstance.Create(ctx, it, this.TemplateArguments)));
                            this.Prefix.IsDereferenced = dereferenced;
                        }
                    }

                    if (this.Binding.Match.IsJoker && !this.IsSink)
                        ctx.ErrorManager.AddError(ErrorCode.ReferenceNotFound, this);
                }

                EntityInstance instance = this.Binding.Match;
                IEntityInstance eval;
                if (instance.Target.IsType() || instance.Target.IsNamespace())
                    eval  = instance;
                else
                    eval = instance.Evaluated(ctx);

                if (this.Prefix != null)
                    eval = eval.TranslateThrough(this.Prefix.Evaluation);

                this.Evaluation = eval;

                //test(ctx);
            }
        }

        public void Validate(ComputationContext ctx)
        {
            if (this.DebugId.Id == 11176)
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
            //return entityInstance;

            if (!ctx.Env.IsPointerOfType(entityInstance) && !ctx.Env.IsReferenceOfType(entityInstance))
                return entityInstance;

            dereferenced = true;

            return entityInstance.TemplateArguments.Single()
                // this is incorrect, just a temporary shortcut
                .Cast<EntityInstance>();

        }

        public NameReference Recreate(IEnumerable<INameReference> arguments)
        {
            var result = new NameReference(this.Prefix, this.Name, arguments, this.IsRoot);
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
                return null;

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