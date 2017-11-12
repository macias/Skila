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
        public static EntityInstance RAW_CreateUnregistered(IEntity target, IEnumerable<IEntityInstance> arguments)
        {
            return new EntityInstance(target, arguments);
        }
        internal static EntityInstance Create(ComputationContext ctx, IEntity target, IEnumerable<INameReference> arguments)
        {
            if (arguments.Any(it => !it.IsBindingComputed))
                throw new ArgumentException("Type parameter binding was not computed.");

            return target.GetInstanceOf(arguments.Select(it => it.Evaluated(ctx)));
        }


#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif
        public static readonly EntityInstance Joker = TypeDefinition.Joker.GetInstanceOf(null);

        public bool IsJoker => this.Target == TypeDefinition.Joker;

        public IEntity Target { get; }
        public TypeDefinition TargetType => this.Target.Cast<TypeDefinition>();
        public TemplateDefinition TargetTemplate => this.Target.Cast<TemplateDefinition>();
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; }
        public bool TargetsTemplateParameter => this.Target.IsType() && this.TargetType.IsTemplateParameter;
        public IEntityInstance Evaluation { get; private set; }
        public bool MissingTemplateArguments => !this.TemplateArguments.Any() && this.Target.Name.Arity > 0;

        private TypeInheritance inheritance;
        public TypeInheritance Inheritance(ComputationContext ctx)
        {
            if (inheritance == null)
            {
                this.TargetType.Evaluated(ctx);
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

        private EntityInstance(IEntity target, IEnumerable<IEntityInstance> arguments)
        {
            if (target == null)
                throw new ArgumentNullException("Instance has to be created for existing entity");

            arguments = arguments ?? Enumerable.Empty<IEntityInstance>();
            this.NameOf = NameReference.Create(null, target.Name.Name, arguments.Select(it => it.NameOf), target: this);
            this.Target = target;
            this.TemplateArguments = arguments.StoreReadOnlyList();
            this.duckVirtualTables = new Dictionary<EntityInstance, VirtualTable>();
        }

        internal void AddDuckVirtualTable(EntityInstance target, VirtualTable vtable)
        {
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
                return this.Target.Name.Name + args;
            }
        }

        public bool IsValueType(ComputationContext ctx)
        {
            return !ctx.Env.IsReferenceOfType(this) && !ctx.Env.IsPointerOfType(this)
                && !(this.Target.IsType() && this.Target.CastType().Modifier.HasHeapOnly);
        }

        public IEntityInstance Evaluated(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                this.Evaluation = this.Target.Evaluated(ctx).TranslateThrough(this);
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
            if (closedTemplate.DebugId.Id == 11091 && this.DebugId.Id == 1029)
            {
                ;
            }
            // if to Coll<T,A> there is IColl<A> (this, dependent instance)
            // then what is to Coll<Int,String> (closedTemplate)? answer: IColl<String>
            // and we compute it here
            // or let's say we have Foo<T> and some method returns T
            // then we have instance Foo<String> (closedTemplate), what T is then? (String)

            // case: "this" is "T", and "closedTemplate" is "Array<Int>" of template "Array<T>"
            if (closedTemplate.TemplateArguments.Any() && this.IsTemplateParameterOf(closedTemplate.TargetTemplate))
            {
                translated = true;
                return closedTemplate.TemplateArguments[this.TargetType.TemplateParameter.Index];
            }
            else
            {
                EntityInstance self = this;
                foreach (IEntityInstance arg in closedTemplate.TemplateArguments)
                {
                    self = self.TranslateThrough(arg);
                }

                if (!self.TemplateArguments.Any())
                {
                    return self;
                }
                else
                {
                    bool trans = false;

                    var trans_arguments = new List<IEntityInstance>();
                    foreach (IEntityInstance arg in self.TemplateArguments)
                        trans_arguments.Add(arg.TranslateThrough(closedTemplate, ref trans));

                    if (trans)
                    {
                        translated = true;
                        return self.Target.GetInstanceOf(trans_arguments);
                    }
                    else
                        return self;
                }
            }
        }

        public TypeMatch MatchesInput(ComputationContext ctx, EntityInstance input, bool allowSlicing)
        {
            return TypeMatcher.Matches(ctx, false, input, this, allowSlicing);
        }

        public TypeMatch MatchesTarget(ComputationContext ctx, IEntityInstance target, bool allowSlicing)
        {
            return target.MatchesInput(ctx, this, allowSlicing);
        }
        public bool IsSame(IEntityInstance other, bool jokerMatchesAll)
        {
            if (this.DebugId.Id == 2023)
            {
                ;
            }
            var other_entity = other as EntityInstance;
            if (other_entity == null)
                return other.IsSame(this, jokerMatchesAll);
            else if (jokerMatchesAll && (this.IsJoker || other_entity.IsJoker))
                return true;
            // note we first compare targets, but then arguments count for instances (not targets)
            else if (this.Target != other_entity.Target || this.TemplateArguments.Count != other_entity.TemplateArguments.Count)
                return false;

            for (int i = 0; i < this.TemplateArguments.Count; ++i)
            {
                if (!other_entity.TemplateArguments[i].IsSame(this.TemplateArguments[i], jokerMatchesAll))
                    return false;
            }

            return true;
        }

        public ConstraintMatch ArgumentMatchesConstraintsOf(ComputationContext ctx, EntityInstance closedTemplate,
            TemplateParameter param)
        {
            if (this.DebugId.Id == 11139)
            {
                ;
            }
            if (param.ConstraintModifier.HasConst && !this.IsImmutableType(ctx))
                return ConstraintMatch.ConstViolation;


            // 'inherits' part of constraint
            foreach (EntityInstance constraint_inherits in param.TranslateInherits(closedTemplate))
            {
                if (TypeMatch.No == TypeMatcher.Matches(ctx, false, this, constraint_inherits, allowSlicing: true))
                    return ConstraintMatch.InheritsViolation;
            }

            {
                VirtualTable vtable = EntityInstanceExtension.BuildDuckVirtualTable(ctx, this, param.AssociatedType.InstanceOf);
                //FunctionDefinition missed_base = TypeDefinitionExtension.PairDerivations(ctx, param.AssociatedType.InstanceOf, this.Target.CastType().NestedFunctions)
                  //  .Where(it => it.Item2 == null).Select(it => it.Item1).FirstOrDefault();
                //if (missed_base != null)
                if (vtable==null)
                    return ConstraintMatch.MissingFunction;
            }


            // 'base-of' part of constraint
            IEnumerable<IEntityInstance> arg_bases = (this.TargetsTemplateParameter
                ? this.TargetType.TemplateParameter.TranslateBaseOf(closedTemplate)
                : Enumerable.Empty<EntityInstance>()
                )
                .Concat(this);

            foreach (EntityInstance constraint_base in param.TranslateBaseOf(closedTemplate))
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
            switch (inversedVariance ? variance.Inverse() : variance)
            {
                case VarianceMode.None:
                    return this.IsSame(input, jokerMatchesAll: true) ? TypeMatch.Pass : TypeMatch.No;
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