using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using Skila.Language.Semantics;

namespace Skila.Language
{
    // instance of an entity, so it could be variable or closed type like "Tuple<Int,Int>"
    // but not open type like "Array<T>" (it is an entity, not an instance of it)
    // this type is divided into its Core part -- holding link to entity and given template arguments
    // and this class (wrapping Core) with context (translation)
    // the reason for the split is we would like to compare Core fast (!) without its context
    // it works, but it looks too complex to be correct solution
    // todo: simplify this

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstance : IEntityInstance
    {
        public static EntityInstance RAW_CreateUnregistered(EntityInstanceCore core, TemplateTranslation translation)
        {
            return new EntityInstance(core, translation);
        }

        /*internal static EntityInstance Create(ComputationContext ctx, EntityInstance targetInstance,
            IEnumerable<INameReference> arguments, MutabilityOverride overrideMutability)
        {
            if (arguments.Any(it => !it.IsBindingComputed))
                throw new ArgumentException("Type parameter binding was not computed.");

            return targetInstance.Build(arguments.Select(it => it.Evaluation.Components), overrideMutability);
        }*/

        internal static EntityInstance Create(ComputationContext ctx, EntityInstance targetInstance,
    IEnumerable<IEntityInstance> arguments, MutabilityOverride overrideMutability)
        {
            return targetInstance.Build(arguments, overrideMutability);
        }

#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(EntityInstance));
#endif
        public static readonly EntityInstance Joker = TypeDefinition.Joker.GetInstance(null,
            overrideMutability: MutabilityOverride.None, translation: TemplateTranslation.Empty);

        public bool IsJoker => this.Target == TypeDefinition.Joker;

        // currently modifier only applies to types mutable/immutable and works as notification
        // that despite the type is immutable we would like to treat is as mutable
        public MutabilityOverride OverrideMutability => this.Core.OverrideMutability;

        public EntityInstanceCore Core { get; }
        public TemplateTranslation Translation { get; }


        public IEntity Target => this.Core.Target;
        public TypeDefinition TargetType => this.Target.CastType();
        public FunctionDefinition TargetFunction => this.Target.CastFunction();
        public TemplateDefinition TargetTemplate => this.Target.Cast<TemplateDefinition>();
        public IReadOnlyList<IEntityInstance> TemplateArguments => this.Core.TemplateArguments;
        public bool TargetsTemplateParameter => this.Target.IsType() && this.TargetType.IsTemplateParameter;
        public TemplateParameter TemplateParameterTarget => this.TargetType.TemplateParameter;
        public EntityInstance Aggregate { get; private set; }
        public IEntityInstance Evaluation { get; private set; }

        private TypeMutability? typeMutabilityCache;

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
        public NameReference NameOf { get; }

        private readonly Dictionary<EntityInstance, VirtualTable> duckVirtualTables;

        private EntityInstance(EntityInstanceCore core,
            TemplateTranslation translation)
        {
            this.Core = core;
            this.Translation = translation;

            this.NameOf = NameReference.Create(null, this.Target.Name.Name, this.TemplateArguments.Select(it => it.NameOf),
                target: this, isLocal: false);
            this.duckVirtualTables = new Dictionary<EntityInstance, VirtualTable>();
        }

        internal void AddDuckVirtualTable(ComputationContext ctx, EntityInstance target, VirtualTable vtable)
        {
            if (vtable == null)
                throw new Exception("Internal error");

            if (target.OverrideMutability.HasFlag(MutabilityOverride.Reassignable))
            {
               target = target.Rebuild(ctx,  target.OverrideMutability ^ MutabilityOverride.Reassignable).Cast<EntityInstance>();
            }
            this.duckVirtualTables.Add(target, vtable);
        }
        public bool TryGetDuckVirtualTable(EntityInstance target, out VirtualTable vtable)
        {
            return this.duckVirtualTables.TryGetValue(target, out vtable);
        }

        public override string ToString()
        {
            string result = this.Core.ToString();
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
                    this.Aggregate = EntityInstance.Joker;
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
            return this.Target.GetInstance(this.TemplateArguments, this.OverrideMutability,
                TemplateTranslation.CombineTraitWithHostParameters(this.Translation, trait: trait)).Cast<EntityInstance>();
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

            if (translation == null)
                return this;

            if (this.TargetsTemplateParameter)
            {
                if (translation.Translate(this.TemplateParameterTarget, out IEntityInstance trans))
                {
                    if (trans != this)
                        translated = true;

                    return trans;
                }

                return this;
            }
            else
            {
                var trans_arguments = new List<IEntityInstance>();
                foreach (IEntityInstance arg in this.TemplateArguments)
                {
                    IEntityInstance trans_arg = arg.TranslateThrough(ref translated, translation);
                    trans_arguments.Add(trans_arg);
                }

                TemplateTranslation combo_translation = TemplateTranslation.Combine(this.Translation, translation);
                EntityInstance result = this.Target.GetInstance(trans_arguments, this.OverrideMutability, combo_translation);

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
        public bool IsExactlySame(IEntityInstance other, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

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

            TypeMutability arg_mutability = this.MutabilityOfType(ctx);
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
                if (this.DebugId == (5, 6455))
                {
                    ;
                }
                TypeMatch match = TypeMatcher.Matches(ctx, this, constraint_inherits,
                    TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true));
                if (TypeMatch.No == match)
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
                if (!arg_bases.Any(it => TypeMatch.No != constraint_base.MatchesTarget(ctx, it, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: true))))
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
            else if ((this.TargetsTemplateParameter && other_entity.TargetsTemplateParameter) || this == other_entity)
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

        public EntityInstance Build(MutabilityOverride mutability)
        {
            if (mutability == MutabilityOverride.Reassignable)
                mutability |= this.OverrideMutability;

            return this.Target.GetInstance(this.TemplateArguments, mutability, this.Translation);
        }

        internal EntityInstance Build(IEnumerable<IEntityInstance> templateArguments, MutabilityOverride overrideMutability)
        {
            TemplateTranslation trans_arg = TemplateTranslation.Create(this.Target, templateArguments);

            return this.Target.GetInstance(templateArguments, overrideMutability,
                TemplateTranslation.Combine(this.Translation, trans_arg));
        }
        internal EntityInstance Build(IEnumerable<INameReference> templateArguments, MutabilityOverride overrideMutability)
        {
            return Build(templateArguments.Select(it => it.Evaluation.Components), overrideMutability);
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
                this.typeMutabilityCache = this.ComputeMutabilityOfType(ctx, new HashSet<IEntityInstance>());
            return this.typeMutabilityCache.Value;
        }

        public bool CoreEquals(IEntityInstance other)
        {
            if (this.GetType() != other.GetType())
                return false;

            EntityInstance instance = other.Cast<EntityInstance>();
            return this.Core == instance.Core;
        }

        public bool ValidateTypeVariance(ComputationContext ctx, INode placement, VarianceMode typeNamePosition)
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
    }
}