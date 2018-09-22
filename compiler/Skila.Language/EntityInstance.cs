using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Tools;

namespace Skila.Language
{
    // instance of an entity, so it could be variable or closed type like "Tuple<Int,Int>"
    // but not open type like "Array<T>" (it is an entity, not an instance of it)
    // this type is divided into its Core part and actual instance (richer)

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed partial class EntityInstance : IEntityInstance
    {
        public static IEqualityComparer<EntityInstance> Comparer { get; }
            = EqualityComparer.Create<EntityInstance>((a, b) => a.IsIdentical(b), x => x.GetHashCode());
        public static IEqualityComparer<IEntityInstance> ComparerI { get; }
            = EqualityComparer.Create<IEntityInstance>((a, b) => a.IsIdentical(b), x => x.GetHashCode());
        public static IEqualityComparer<EntityInstance> CoreComparer { get; }
            = EqualityComparer.Create<EntityInstance>((a, b) => a.HasSameCore(b), x => x.core.GetHashCode());

        internal static EntityInstance CreateUnregistered(RuntimeCore core, IEntity target,
            TemplateTranslation translation,
            TypeMutability overrideMutability,
            Lifetime lifetime)
        {
            var instance = new EntityInstance(core, target, translation, overrideMutability, lifetime);

            return instance;
        }


#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(EntityInstance));
#endif

        public bool IsJoker => this.Target == Environment.JokerType;

        // currently modifier only applies to types mutable/immutable and works as notification
        // that despite the type is immutable we would like to treat is as mutable
        public TypeMutability OverrideMutability { get; } // we use bool flag instead full EntityModifer because so far we don't have other modifiers

        public TemplateTranslation Translation { get; }

        public IEntity Target { get; }
        public Lifetime Lifetime { get; }

        public TypeDefinition TargetType => this.Target.CastType();
        public FunctionDefinition TargetFunction => this.Target.CastFunction();
        public TemplateDefinition TargetTemplate => this.Target.Cast<TemplateDefinition>();
        public IReadOnlyList<IEntityInstance> TemplateArguments => this.Translation.PrimaryArguments;
        public bool TargetsTemplateParameter => this.Target.IsType() && this.TargetType.IsTemplateParameter;
        public TemplateParameter TemplateParameterTarget => this.TargetType.TemplateParameter;
        public EntityInstance Aggregate { get; private set; }
        public IEntityInstance Evaluation { get; private set; }

        private TypeMutability? typeMutabilityCache;
        private TypeMutability? surfaceTypeMutabilityCache;

        private TypeInheritance inheritance;
        public TypeInheritance Inheritance(ComputationContext ctx)
        {
            if (inheritance == null)
            {
                if (!this.TargetType.IsInheritanceComputed)
                    this.TargetType.Surfed(ctx);
                this.inheritance = this.TargetType.Inheritance.TranslateThrough(ctx, this);
            }
            return inheritance;
        }

        public bool IsTypeImplementation => this.Target.IsType() && this.TargetType.IsTypeImplementation;

        INameReference IEntityInstance.NameOf => this.NameOf;
        INameReference IEntityInstance.PureNameOf => this.PureNameOf;
        private readonly Later<NameReference> nameOf;
        private readonly Later<NameReference> pureNameOf;
        public NameReference NameOf => this.nameOf.Value;
        public NameReference PureNameOf => this.pureNameOf.Value;

        private readonly RuntimeCore core;

        private EntityInstance(RuntimeCore core, IEntity target,
            TemplateTranslation translation, TypeMutability overrideMutability,
            Lifetime lifetime)
        {
            if (target == null)
                throw new ArgumentNullException("Instance has to be created for existing entity");

            this.core = core;
            this.Target = target;
            this.Translation = translation;

            this.Lifetime = lifetime;

            this.OverrideMutability = overrideMutability;

            this.nameOf = Later.Create(() => NameReference.Create(
                OverrideMutability,
                null, this.Target.Name.Name, this.TemplateArguments.Select(it => it.NameOf),
                target: this, isLocal: false));
            this.pureNameOf = Later.Create(() => NameReference.Create(
                null, this.Target.Name.Name, this.TemplateArguments.Select(it => it.NameOf),
                target: this, isLocal: false));
        }

        internal void AddDuckVirtualTable(ComputationContext ctx, EntityInstance target, VirtualTable vtable)
        {
            this.core.AddDuckVirtualTable(ctx, target, vtable);
        }
        public bool TryGetDuckVirtualTable(EntityInstance target, out VirtualTable vtable)
        {
            return this.core.TryGetDuckVirtualTable(target, out vtable);
        }

        public override string ToString()
        {
            string result;

            if (this.IsJoker)
                result = "<<*>>";
            else
            {
                string args = "";
                if (this.TemplateArguments.Any())
                    args = "<" + this.TemplateArguments.Select(it => it.ToString()).Join(",") + ">";
                result = $"{this.OverrideMutability.StringPrefix()}{this.Target.Name.Name}{args}";
            }

            if (this.Translation != null)
                result += $" {this.Translation}";
            return result;
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return !ctx.Env.IsReferenceOfType(this) && !ctx.Env.IsPointerOfType(this)
                && !(this.Target.IsType() && this.TargetType.Modifier.HasHeapOnly);
        }

        public IEntityInstance Evaluated(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                IEntityInstance eval;
                if (!this.IsJoker && this.Target is TemplateDefinition)
                    eval = this.TargetTemplate.Evaluation.Components;
                else
                    eval = this.Target.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

                this.Evaluation = eval.TranslateThrough(this);
                if (this.Evaluation.IsJoker)
                    this.Aggregate = Environment.JokerInstance;
                else
                {
                    EntityInstance aggregate = this.Target.Evaluation.Aggregate;
                    this.Aggregate = aggregate.TranslateThrough(this);
                }
            }

            return this.Evaluation;
        }

        public IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated, TemplateTranslation closedTranslation)
        {
            return openTemplate.TranslateThrough(ref translated, closedTranslation ?? this.Translation);
        }

        public bool IsTemplateParameterOf(TemplateDefinition template)
        {
            return this.TargetsTemplateParameter && template.ContainsElement(this.TargetType);
        }

        internal EntityInstance TranslateThroughTraitHost(TypeDefinition trait)
        {
            TemplateTranslation combined = TemplateTranslation.OverriddenTraitWithHostParameters(this.Translation, trait: trait);

            return this.Target.GetInstance(this.OverrideMutability, combined, Lifetime.Timeless);
        }

        public EntityInstance TranslateThrough(EntityInstance closedTemplate)
        {
            bool dummy = false;
            return TranslateThrough(ref dummy, closedTemplate.Translation).Cast<EntityInstance>();
        }

        public IEntityInstance TranslateThrough(ref bool translated, TemplateTranslation translation)
        {
            // consider:
            // Coll<T,A> implements IColl<A>
            // and then you have Coll<Int,String> (closedTemplate) -- so how does IColl looks now?
            // IColl<String> (since A maps to String)
            // and we compute it here
            // or let's say we have type Foo<T> and one of its methods returns T
            // then we have instance Foo<String> (closedTemplate), what T is then? (String)

            if (translation == null || translation == TemplateTranslation.Empty)
                return this;

            if (this.TargetsTemplateParameter)
            {
                if (translation.Translate(this.TemplateParameterTarget, out IEntityInstance trans))
                {
                    if (!this.IsIdentical(trans))
                        translated = true;

                    return trans;
                }

                return this;
            }
            else
            {
                TemplateTranslation combo_translation = TemplateTranslation.Translated(this.Translation, translation, ref translated);

                EntityInstance result = this.Target.GetInstance(this.OverrideMutability, combo_translation, this.Lifetime);

                return result;
            }
        }

        public TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, TypeMatching matching)
        {
            return TypeMatcher.Matches(ctx, input, this, matching);
        }

        public TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, TypeMatching matching)
        {
            return target.MatchesInput(ctx, this, matching);
        }

        public bool HasSameCore(IEntityInstance other)
        {
            if (other is EntityInstance other_instance)
                return this.HasSameCore(other_instance);
            else
                return false;
        }

        public bool HasSameCore(EntityInstance other)
        {
            return this.core.Equals(other.core);
        }

        public bool IsIdentical(IEntityInstance other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;

            if (other is EntityInstance other_instance)
                return this.HasSameCore(other_instance) && this.OverrideMutability == other_instance.OverrideMutability;
            else
                return false;
        }

        public bool IsExactlySame(IEntityInstance other, bool jokerMatchesAll)
        {
            bool identical = this.IsIdentical(other);
            if (!jokerMatchesAll || identical)
                return identical;

            var other_entity = other as EntityInstance;
            if (other_entity == null)
                return other.IsExactlySame(this, jokerMatchesAll);

            if (this.IsJoker || other_entity.IsJoker)
                return true;

            if (this.OverrideMutability != other_entity.OverrideMutability)
                return false;
            // note we first compare targets, but then arguments count for instances (not targets)
            if (this.Target != other_entity.Target || this.TemplateArguments.Count != other_entity.TemplateArguments.Count)
                return false;

            for (int i = 0; i < this.TemplateArguments.Count; ++i)
            {
                if (!other_entity.TemplateArguments[i].IsExactlySame(this.TemplateArguments[i], jokerMatchesAll))
                    return false;
            }

            return true;
        }
        public bool HasExactlySameTarget(IEntityInstance other, bool jokerMatchesAll)
        {
            var other_entity = other as EntityInstance;
            if (other_entity == null)
                return other.HasExactlySameTarget(this, jokerMatchesAll);

            if (jokerMatchesAll && (this.IsJoker || other_entity.IsJoker))
                return true;

            if (this.OverrideMutability != other_entity.OverrideMutability)
                return false;
            // note we first compare targets, but then arguments count for instances (not targets)
            if (this.Target != other_entity.Target)
                return false;

            return true;
        }

        public ConstraintMatch ArgumentMatchesParameterConstraints(ComputationContext ctx, EntityInstance closedTemplate,
            TemplateParameter param)
        {
            if (this.TargetsTemplateParameter && this.TemplateParameterTarget == param)
                return ConstraintMatch.Yes;

            TypeMutability arg_mutability = this.SurfaceMutabilityOfType(ctx);
            if (param.Constraint.Modifier.HasConst
                && arg_mutability != TypeMutability.ForceConst && arg_mutability != TypeMutability.ConstAsSource)
                return ConstraintMatch.MutabilityViolation;
            else if (param.Constraint.Modifier.HasMutable && arg_mutability != TypeMutability.ForceMutable)
                return ConstraintMatch.MutabilityViolation;
            else if (param.Constraint.Modifier.HasReassignable && !arg_mutability.HasFlag(TypeMutability.Reassignable))
                return ConstraintMatch.AssignabilityViolation;

            // 'inherits' part of constraint
            foreach (EntityInstance constraint_inherits in param.Constraint.TranslateInherits(closedTemplate))
            {
                TypeMatch match = TypeMatcher.Matches(ctx, this, constraint_inherits,
                    TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true));
                if (match.IsMismatch())
                    return ConstraintMatch.InheritsViolation;
            }

            {
                VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, this, param.AssociatedType.InstanceOf,
                    allowPartial: false);
                if (vtable == null)
                    return ConstraintMatch.MissingFunction;
            }


            // 'base-of' part of constraint
            IEnumerable<IEntityInstance> arg_bases = (this.TargetsTemplateParameter
                ? this.TargetType.TemplateParameter.Constraint.TranslateBaseOf(closedTemplate)
                : Enumerable.Empty<EntityInstance>()
                )
                .Concat(this);

            foreach (EntityInstance constraint_base in param.Constraint.TranslateBaseOf(closedTemplate))
            {
                if (!arg_bases.Any(it => !constraint_base.MatchesTarget(ctx, it, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true))
                    .IsMismatch()))
                    return ConstraintMatch.BaseViolation;
            }

            return ConstraintMatch.Yes;
        }
        public TypeMatch TemplateMatchesTarget(ComputationContext ctx, IEntityInstance target, VarianceMode variance, TypeMatching matching)
        {
            return target.TemplateMatchesInput(ctx, this, variance, matching);
        }

        public TypeMatch TemplateMatchesInput(ComputationContext ctx, EntityInstance input,
            VarianceMode variance, TypeMatching matching)
        {
            // todo: this is correct but it is just a hack really, because we should check constraints
            // and if they are compatible we should return match
            if (input.TargetsTemplateParameter && this.TargetsTemplateParameter
                && !input.TargetTemplate.Constraints.Any() && !this.TargetTemplate.Constraints.Any())
                return TypeMatch.Same;

            matching.Position = matching.Position.Flipped(variance);
            switch (matching.Position)
            {
                case VarianceMode.None:
                    return this.IsExactlySame(input, jokerMatchesAll: true) ? TypeMatch.Same : TypeMatch.No;
                case VarianceMode.In:
                    return TypeMatcher.Matches(ctx, this, input, matching);
                case VarianceMode.Out:
                    return TypeMatcher.Matches(ctx, input, this, matching);
            }

            throw new NotImplementedException();
        }

        public bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor)
        {
            return this.Inheritance(ctx).OrderedAncestorsIncludingObject.Contains(ancestor);
        }

        public bool IsStrictAncestorOf(ComputationContext ctx, IEntityInstance descendant)
        {
            return descendant.IsStrictDescendantOf(ctx, ancestor: this);
        }

        // please remember we have to allow specialization of the function
        // template types (T and V) are not distinct
        // template type (T) and concrete type (String) are (!) distinct (specialization)
        // two concrete types (String and Object) are distinct (here is the example of specialization as well)
        public bool IsOverloadDistinctFrom(IEntityInstance other)
        {
            EntityInstance other_entity = other as EntityInstance;
            if (other_entity == null)
                return other.IsOverloadDistinctFrom(this);
            else if ((this.TargetsTemplateParameter && other_entity.TargetsTemplateParameter) || this.IsIdentical(other_entity))
                return false;
            else if (this.Target != other_entity.Target)
                return true;

            for (int i = 0; i < this.TemplateArguments.Count; ++i)
            {
                if (this.TemplateArguments[i].IsOverloadDistinctFrom(other_entity.TemplateArguments[i]))
                    return true;
            }

            return false;

        }

        public EntityInstance Build(TypeMutability mutability)
        {
            if (mutability == TypeMutability.Reassignable)
                mutability |= this.OverrideMutability;

            if (mutability == this.OverrideMutability)
                return this;
            else
                return this.Target.GetInstance(mutability, this.Translation, this.Lifetime);

        }

        public EntityInstance Build(TypeMutability mutability, Lifetime lifetime)
        {
            if (mutability == TypeMutability.Reassignable)
                mutability |= this.OverrideMutability;

            if (mutability == this.OverrideMutability && this.Lifetime == lifetime)
                return this;
            else
                return this.Target.GetInstance(mutability, this.Translation, lifetime);
        }

        public EntityInstance Build(Lifetime lifetime)
        {
            if (this.Lifetime == lifetime)
                return this;
            else
                return this.Target.GetInstance(this.OverrideMutability, this.Translation, lifetime);
        }

        internal EntityInstance Build(IEnumerable<IEntityInstance> templateArguments, TypeMutability overrideMutability)
        {
            TemplateTranslation combined = this.Translation.Overridden(templateArguments);

            return this.Target.GetInstance(overrideMutability, combined, this.Lifetime);
        }

        internal EntityInstance BuildNoArguments()
        {
            return Build(this.Target.Name.Parameters.Select(_ => (IEntityInstance)null), this.OverrideMutability);
        }

        internal EntityInstance Build(IEnumerable<TemplateArgument> templateArguments, TypeMutability overrideMutability)
        {
            return Build(templateArguments.Select(it => it.TypeName.Evaluation.Components), overrideMutability);
        }

        public IEntityInstance Map(Func<EntityInstance, IEntityInstance> func)
        {
            return func(this);
        }

        public IEnumerable<EntityInstance> EnumerateAll()
        {
            yield return this;
        }

        public TypeMutability MutabilityOfType(ComputationContext ctx)
        {
            if (!this.typeMutabilityCache.HasValue)
                this.typeMutabilityCache = this.ComputeMutabilityOfType(ctx, new HashSet<IEntityInstance>(EntityInstance.ComparerI));
            return this.typeMutabilityCache.Value;
        }

        public TypeMutability SurfaceMutabilityOfType(ComputationContext ctx)
        {
            if (!this.surfaceTypeMutabilityCache.HasValue)
                this.surfaceTypeMutabilityCache = this.ComputeSurfaceMutabilityOfType(ctx);
            return this.surfaceTypeMutabilityCache.Value;
        }

        public bool ValidateTypeVariance(ComputationContext ctx, IOwnedNode placement, VarianceMode typeNamePosition)
        {
            // Programming in Scala, 2nd ed, p. 399 (all errors are mine)

            TypeDefinition typedef = this.TargetType;

            if (typedef.IsTemplateParameter)
            {
                TemplateParameter param = typedef.TemplateParameter;
                TemplateDefinition template = param.EnclosingScope<TemplateDefinition>();
                if (placement.EnclosingScopesToRoot().Contains(template))
                {
                    bool covariant_in_immutable = param.Variance == VarianceMode.Out
                        && (template.IsFunction() || template.CastType().InstanceOf.MutabilityOfType(ctx) == TypeMutability.ConstAsSource);

                    // don't report errors for covariant types which are used in immutable template types
                    if (!covariant_in_immutable &&
                        typeNamePosition.PositionCollides(param.Variance))
                        return false;
                }
            }
            else
                for (int i = 0; i < typedef.Name.Parameters.Count; ++i)
                {
                    if (!this.TemplateArguments[i].ValidateTypeVariance(ctx,
                        placement,
                        typeNamePosition.Flipped(typedef.Name.Parameters[i].Variance)))
                        return false;
                }

            return true;
        }

        public override int GetHashCode()
        {
            return this.core.GetHashCode() ^ this.OverrideMutability.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EntityInstance);
        }

        public bool Equals(EntityInstance obj)
        {
            return this.IsIdentical(obj);
        }

        #region disabling default equality methods

        public static bool operator ==(EntityInstance a, EntityInstance b)
        {
            if (Object.ReferenceEquals(b, null))
                return Object.ReferenceEquals(a, null);
            else
                throw new InvalidOperationException("Use specialized methods");
        }
        public static bool operator !=(EntityInstance a, EntityInstance b)
        {
            return !(a == b);
        }
        public static bool operator ==(EntityInstance a, IEntityInstance b)
        {
            if (b is EntityInstance b_instance)
                return a == b_instance;
            else
                throw new InvalidOperationException("Use specialized methods");
        }
        public static bool operator !=(EntityInstance a, IEntityInstance b)
        {
            return !(a == b);
        }
        public static bool operator ==(IEntityInstance a, EntityInstance b)
        {
            return (b == a);
        }
        public static bool operator !=(IEntityInstance a, EntityInstance b)
        {
            return !(a == b);
        }

        #endregion

    }
}