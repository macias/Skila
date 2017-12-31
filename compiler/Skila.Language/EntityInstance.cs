using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    // instance of an entity, so it could be variable or closed type like "Tuple<Int,Int>"
    // but not open type like "Array<T>" (it is an entity, not an instance of it)

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstance : IEntityInstance
    {
        public static EntityInstance RAW_CreateUnregistered(IEntity target, EntityInstanceSignature signature)
        {
            return new EntityInstance(target, signature.TemplateArguments, signature.OverrideMutability, signature.Translation);
        }

        internal static EntityInstance Create(ComputationContext ctx, EntityInstance targetInstance,
            IEnumerable<INameReference> arguments, bool overrideMutability)
        {
            if (arguments.Any(it => !it.IsBindingComputed))
                throw new ArgumentException("Type parameter binding was not computed.");

            return targetInstance.Target.GetInstance(arguments.Select(it => it.Evaluated(ctx)), overrideMutability,
                TemplateTranslation.Combine(targetInstance.Translation, targetInstance.Target.Name.Parameters, arguments));
        }

#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif
        public static readonly EntityInstance Joker = TypeDefinition.Joker.GetInstance(null, overrideMutability: false, translation: null);

        public bool IsJoker => this.Target == TypeDefinition.Joker;

        // currently modifier only applies to types mutable/immutable and works as notification
        // that despite the type is immutable we would like to treat is as mutable
        public bool OverrideMutability { get; } // we use bool flag instead full EntityModifer because so far we don't have other modifiers

        internal TemplateTranslation Translation { get; }

        public IEntity Target { get; }
        public TypeDefinition TargetType => this.Target.CastType();
        public TemplateDefinition TargetTemplate => this.Target.Cast<TemplateDefinition>();
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; }
        public bool TargetsTemplateParameter => this.Target.IsType() && this.TargetType.IsTemplateParameter;
        public TemplateParameter TemplateParameterTarget => this.TargetType.TemplateParameter;
        public EntityInstance Aggregate { get; private set; }
        public IEntityInstance Evaluation { get; private set; }
        public bool MissingTemplateArguments => !this.TemplateArguments.Any() && this.Target.Name.Arity > 0;

        private TypeInheritance inheritance;
        public TypeInheritance Inheritance(ComputationContext ctx)
        {
            if (inheritance == null)
            {
                if (!this.TargetType.IsInheritanceComputed)
                    this.TargetType.Surfed(ctx);
                this.inheritance = this.TargetType.Inheritance.TranslateThrough(this);
            }
            return inheritance;
        }

        public bool IsTypeImplementation => this.Target.IsType() && this.TargetType.IsTypeImplementation;

        INameReference IEntityInstance.NameOf => this.NameOf;
        public NameReference NameOf { get; }

        public bool DependsOnTypeParameter_UNUSED
        {
            get
            {
                return this.TargetsTemplateParameter || this.TemplateArguments.Any(it => it.DependsOnTypeParameter_UNUSED);
            }
        }

        private readonly Dictionary<EntityInstance, VirtualTable> duckVirtualTables;

        private EntityInstance(IEntity target, IEnumerable<IEntityInstance> arguments, bool overrideMutability,
            TemplateTranslation translation)
        {
            if (target == null)
                throw new ArgumentNullException("Instance has to be created for existing entity");

            arguments = arguments ?? Enumerable.Empty<IEntityInstance>();
            this.NameOf = NameReference.Create(null, target.Name.Name, arguments.Select(it => it.NameOf), target: this);
            this.Target = target;
            this.TemplateArguments = arguments.StoreReadOnlyList();
            this.duckVirtualTables = new Dictionary<EntityInstance, VirtualTable>();
            this.OverrideMutability = overrideMutability;
            this.Translation = translation;

            if (this.DebugId.Id == 4835)
            {
                ;
            }

            if (this.OverrideMutability)
            {
                ;
            }
        }

        internal void AddDuckVirtualTable(EntityInstance target, VirtualTable vtable)
        {
            if (vtable == null)
                throw new Exception("Internal error");
            this.duckVirtualTables.Add(target, vtable);
        }
        public bool TryGetDuckVirtualTable(EntityInstance target, out VirtualTable vtable)
        {
            return this.duckVirtualTables.TryGetValue(target, out vtable);
        }

        public override string ToString()
        {
            if (this.IsJoker)
                return "<<*>>";
            else
            {
                string args = "";
                if (this.TemplateArguments.Any())
                    args = "<" + this.TemplateArguments.Select(it => it.ToString()).Join(",") + ">";
                string result = (this.OverrideMutability ? "mutable " : "") + this.Target.Name.Name + args;
                if (this.Translation != null)
                    result += $" [{this.Translation}]";
                return result;
            }
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
                IEntityInstance eval = this.Target.Evaluated(ctx);
                this.Evaluation = eval.TranslateThrough(this);
                if (this.Evaluation.IsJoker)
                    this.Aggregate = EntityInstance.Joker;
                else
                    this.Aggregate = this.Target.Evaluation.Aggregate.TranslateThrough(this);
            }
            return this.Evaluation;
        }

        public bool IsTemplateParameterOf(TemplateDefinition template)
        {
            return this.TargetsTemplateParameter && template.ContainsElement(this.TargetType);
        }

        public EntityInstance TranslateThrough(EntityInstance closedTemplate)
        {
            return (this as IEntityInstance).TranslateThrough(closedTemplate).Cast<EntityInstance>();
        }

        public IEntityInstance TranslationOf(IEntityInstance openTemplate, ref bool translated)
        {
            return openTemplate.TranslateThrough(this, ref translated);
        }

        public IEntityInstance TranslateThrough(EntityInstance closedTemplate, ref bool translated)
        {
            if (closedTemplate.DebugId.Id == 3386 && this.DebugId.Id == 486)
            {
                ;
            }
            // if to Coll<T,A> there is IColl<A> (this, dependent instance)
            // then what is to Coll<Int,String> (closedTemplate)? answer: IColl<String>
            // and we compute it here
            // or let's say we have type Foo<T> and one of its methods returns T
            // then we have instance Foo<String> (closedTemplate), what T is then? (String)

            // case: "this" is "T", and "closedTemplate" is "Array<Int>" of template "Array<T>"
            if (closedTemplate.TemplateArguments.Any() && this.IsTemplateParameterOf(closedTemplate.TargetTemplate))
            {
                translated = true;
                return closedTemplate.TemplateArguments[this.TemplateParameterTarget.Index];
            }

            {
                if (closedTemplate.Translation != null && this.TargetsTemplateParameter
                                && closedTemplate.Translation.Translate(this.TemplateParameterTarget, out IEntityInstance trans))
                {
                    translated = true;
                    return trans.TranslateThrough(closedTemplate, ref translated);
                }
            }

            EntityInstance self = this;

            foreach (IEntityInstance arg in closedTemplate.TemplateArguments)
            {
                self = self.TranslateThrough(arg);
                if (this!=self)
                {
                    ;
                }
            }
            
            TemplateTranslation translation = TemplateTranslation.Combine(self.Translation, closedTemplate.Translation);

            if (self.TemplateArguments.Any())
            {
                bool trans = false;

                var trans_arguments = new List<IEntityInstance>();
                foreach (IEntityInstance arg in self.TemplateArguments)
                    trans_arguments.Add(arg.TranslateThrough(closedTemplate, ref trans));

                if (trans)
                {
                    translated = true;
                    return self.Target.GetInstance(trans_arguments, this.OverrideMutability, translation);
                }
            }

            self = self.Target.GetInstance(self.TemplateArguments, self.OverrideMutability, translation);
            return self;
        }

        public TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing)
        {
            return TypeMatcher.Matches(ctx, false, input, this, allowSlicing);
        }

        public TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing)
        {
            return target.MatchesInput(ctx, this, allowSlicing);
        }
        public bool IsSame( IEntityInstance other, bool jokerMatchesAll)
        {
            if (!jokerMatchesAll)
                return this == other;

            if (this.DebugId.Id == 2023)
            {
                ;
            }
            var other_entity = other as EntityInstance;
            if (other_entity == null)
                return other.IsSame(this, jokerMatchesAll);

            if (this.IsJoker || other_entity.IsJoker)
                return true;

            if (this.OverrideMutability != other_entity.OverrideMutability)
                return false;
            // note we first compare targets, but then arguments count for instances (not targets)
            if (this.Target != other_entity.Target || this.TemplateArguments.Count != other_entity.TemplateArguments.Count)
                return false;

            for (int i = 0; i < this.TemplateArguments.Count; ++i)
            {
                if (!other_entity.TemplateArguments[i].IsSame(this.TemplateArguments[i], jokerMatchesAll))
                    return false;
            }

            return true;
        }

        public ConstraintMatch ArgumentMatchesParameterConstraints(ComputationContext ctx, EntityInstance closedTemplate,
            TemplateParameter param)
        {
            if (this.DebugId.Id == 11139)
            {
                ;
            }
            if (param.Constraint.Modifier.HasConst && !this.IsImmutableType(ctx))
                return ConstraintMatch.ConstViolation;


            // 'inherits' part of constraint
            foreach (EntityInstance constraint_inherits in param.Constraint.TranslateInherits(closedTemplate))
            {
                if (TypeMatch.No == TypeMatcher.Matches(ctx, false, this, constraint_inherits, allowSlicing: true))
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
                if (!arg_bases.Any(it => TypeMatch.No != constraint_base.MatchesTarget(ctx, it, allowSlicing: true)))
                    return ConstraintMatch.BaseViolation;
            }

            return ConstraintMatch.Yes;
        }
        public TypeMatch TemplateMatchesTarget(ComputationContext ctx, bool inversedVariance, IEntityInstance target, VarianceMode variance, bool allowSlicing)
        {
            return target.TemplateMatchesInput(ctx, inversedVariance, this, variance, allowSlicing);
        }

        public TypeMatch TemplateMatchesInput(ComputationContext ctx, bool inversedVariance, EntityInstance input, VarianceMode variance, bool allowSlicing)
        {
            switch (inversedVariance ? variance.Inversed() : variance)
            {
                case VarianceMode.None:
                    return this.IsSame(input, jokerMatchesAll: true) ? TypeMatch.Same : TypeMatch.No;
                case VarianceMode.In:
                    return TypeMatcher.Matches(ctx, !inversedVariance, this, input, allowSlicing);
                case VarianceMode.Out:
                    return TypeMatcher.Matches(ctx, !inversedVariance, input, this, allowSlicing);
            }

            throw new NotImplementedException();
        }

        public bool IsStrictDescendantOf(ComputationContext ctx, EntityInstance ancestor)
        {
            return this.Inheritance(ctx).AncestorsIncludingObject.Contains(ancestor);
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

        public IEnumerable<EntityInstance> Enumerate()
        {
            yield return this;
        }

    }
}