using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Comparers;
using Skila.Language.Expressions.Literals;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeDefinition : TypeContainerDefinition
    {
        public static TypeDefinition Create(EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            bool allowSlicing,
            IEnumerable<NameReference> parents = null, IEnumerable<INode> features = null)
        {
            return new TypeDefinition(modifier, allowSlicing, name, constraints,
                parents, features,
                typeParameter: null, includes: null);
        }
        public static TypeDefinition CreateFunctionInterface(NameDefinition name)
        {
            return new TypeDefinition(EntityModifier.Interface,
                false, name, null, new[] { NameFactory.IObjectTypeReference() },
                features: null,
                typeParameter: null, includes: null);
        }

        // used for creating embedded type definitions of type parameters, e.g. Tuple<T1,T2>, here we create T1 and T2
        public static TypeDefinition CreateTypeParameter(TemplateParameter typeParameter)
        {
            EntityModifier modifier = typeParameter.Constraint.Modifier;
            if (typeParameter.Constraint.HasFunctions.Any())
                modifier |= EntityModifier.Protocol;
            else
                modifier |= EntityModifier.Base | EntityModifier.Interface;
            if (!modifier.HasConst)
                modifier |= EntityModifier.Mutable;
            return new TypeDefinition(modifier, false, NameDefinition.Create(typeParameter.Name), null,
                typeParameter.Constraint.InheritsNames, typeParameter.Constraint.HasFunctions,
                typeParameter, includes: null);
        }


        public static TypeDefinition Joker { get; } = TypeDefinition.Create(EntityModifier.None,
            NameDefinition.Create(NameFactory.JokerTypeName), null, allowSlicing: true);

        public bool IsTypeImplementation => !this.IsInterface && !this.IsProtocol;
        public bool IsInterface => this.Modifier.HasInterface;
        public bool IsTrait => this.Modifier.HasTrait;
        public bool IsProtocol => this.Modifier.HasProtocol;

        public bool IsJoker => this.Name.Name == NameFactory.JokerTypeName;

        public TemplateParameter TemplateParameter { get; }
        public bool IsTemplateParameter => this.TemplateParameter != null;
        public IReadOnlyCollection<NameReference> ParentNames { get; }
        //public IEnumerable<EntityInstance> ParentTypes => this.ParentNames.Select(it => it.Binding.Match).WhereType<EntityInstance>();
        public TypeInheritance Inheritance { get; private set; }
        public bool IsInheritanceComputed => this.Inheritance != null;

        private bool isEvaluated;

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(this.ParentNames)
            .Where(it => it != null);

        public bool AllowSlicedSubstitution { get; }

        public VirtualTable InheritanceVirtualTable { get; private set; }
        public DerivationTable DerivationTable { get; private set; }

        private IReadOnlyCollection<EntityInstance> availableEntities;
        public override IEnumerable<EntityInstance> AvailableEntities => this.availableEntities;

        public IEnumerable<TypeDefinition> AssociatedTraits => this.IsJoker ? Enumerable.Empty<TypeDefinition>()
            : this.Owner.NestedTypes().Where(it => this.isHostOfTrait(it));
        public TypeDefinition AssociatedHost => this.Owner.NestedTypes().FirstOrDefault(it => it.isHostOfTrait(this));

        private TypeDefinition(EntityModifier modifier,
            bool allowSlicing,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<NameReference> parents,
            IEnumerable<INode> features,
            TemplateParameter typeParameter,
            IEnumerable<NameReference> includes) : base(modifier, name, constraints, includes)
        {
            this.AllowSlicedSubstitution = allowSlicing;
            this.TemplateParameter = typeParameter;
            this.ParentNames = (parents ?? Enumerable.Empty<NameReference>()).StoreReadOnly();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            features?.ForEach(it => AddNode(it));

            // all nodes have to be attached at this point
            setupConstructors();

            constructionCompleted = true;
        }

        public override void Surf(ComputationContext ctx)
        {
            base.Surf(ctx);

            this.compute(ctx);
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (!this.isEvaluated)
                compute(ctx);

            IsComputed = true;
        }

        private void compute(ComputationContext ctx)
        {
            foreach (TypeDefinition trait in this.AssociatedTraits)
                trait.computeAncestors(ctx, new HashSet<TypeDefinition>());
            computeAncestors(ctx, new HashSet<TypeDefinition>());

            if (this.Modifier.HasEnum)
            {
                // finally we are at point when we know from which offset we can start setting ids for enum cases

                FunctionDefinition zero_constructor = this.NestedFunctions.Single(it => it.IsZeroConstructor() && it.Modifier.HasStatic);
                if (zero_constructor.IsComputed)
                    throw new System.Exception("Internal error -- we cannot alter the body after the function was already computed");

                int enum_ord = this.InstanceOf.PrimaryAncestors(ctx).Select(it => it.TargetType)
                    .Sum(it => it.NestedFields.Count(f => f.Modifier.HasEnum));

                foreach (VariableDeclaration decl in this.NestedFields.Where(it => it.Modifier.HasEnum))
                    zero_constructor.UserBody.Append(decl.CreateFieldInitCall(NatLiteral.Create($"{enum_ord++}")));

            }

            // base method -> derived (here) method
            var virtual_mapping = new VirtualTable(isPartial: false);
            foreach (EntityInstance parent_instance in this.Inheritance.MinimalParentsIncludingObject
                .Concat(this.AssociatedTraits.SelectMany(it => it.Inheritance.MinimalParentsIncludingObject))
                .Distinct()
                .Reverse())
            {
                virtual_mapping.OverrideWith(parent_instance.TargetType.InheritanceVirtualTable);
            }

            IEnumerable<FunctionDefinition> all_nested_functions = this.AllNestedFunctions
                .Concat(this.AssociatedTraits.SelectMany(it => it.AllNestedFunctions));

            // derived (here) method -> base methods
            Dictionary<FunctionDefinition, List<FunctionDefinition>> derivation_mapping = all_nested_functions
                .Where(it => it.Modifier.HasOverride)
                .ToDictionary(it => it, it => new List<FunctionDefinition>());

            var inherited_member_instances = new HashSet<EntityInstance>();

            var missing_func_implementations = new List<FunctionDefinition>();

            foreach (EntityInstance ancestor in this.Inheritance.OrderedAncestorsIncludingObject
                .Concat(this.AssociatedTraits.SelectMany(it => it.Inheritance.OrderedAncestorsIncludingObject))
                .Distinct())
            {
                // special case are properties -- properties are in fact not inherited, their accessors are
                // however user sees properties, so we get members here (including properties), but when we compute
                // what function overrode which, we use really functions, and only having them in hand we check if 
                // they are property accessors
                IEnumerable<EntityInstance> members = (ancestor.TargetType.AvailableEntities ?? Enumerable.Empty<EntityInstance>())
                    .Where(it => it.Target is IMember);

                foreach (EntityInstance entity_instance in members
                    .Where(it => it.Target.Modifier.HasPublic || it.Target.Modifier.HasProtected)
                    .Where(it => !(it.Target is FunctionDefinition func) || !func.IsAnyConstructor()))
                {
                    EntityInstance translated = entity_instance.TranslateThrough(ancestor);
                    inherited_member_instances.Add(translated);
                }

                foreach (FunctionDerivation deriv_info in TypeDefinitionExtension.PairDerivations(ctx, ancestor, all_nested_functions))
                {
                    if (deriv_info.Derived == null)
                    {
                        // we can skip implementation or abstract signature of the function in the abstract type
                        if (deriv_info.Base.Modifier.RequiresOverride && !this.Modifier.IsAbstract)
                            missing_func_implementations.Add(deriv_info.Base);
                    }
                    else
                    {
                        {
                            EntityInstance base_instance;
                            if (deriv_info.Base.IsPropertyAccessor(out Property base_property))
                                base_instance = base_property.InstanceOf.TranslateThrough(ancestor);
                            else
                                base_instance = deriv_info.Base.InstanceOf.TranslateThrough(ancestor);

                            inherited_member_instances.Remove(base_instance);
                        }

                        if (deriv_info.Derived.Modifier.HasOverride)
                        {
                            derivation_mapping[deriv_info.Derived].Add(deriv_info.Base);
                            // user does not have to repeat "pinned" all the time, but for easier tracking of pinned methods
                            // we add it automatically
                            if (deriv_info.Base.Modifier.HasPinned)
                                deriv_info.Derived.SetModifier(deriv_info.Derived.Modifier | EntityModifier.Pinned);

                            if (deriv_info.Base.Modifier.HasHeapOnly != deriv_info.Derived.Modifier.HasHeapOnly)
                                ctx.AddError(ErrorCode.HeapRequirementChangedOnOverride, deriv_info.Derived);
                        }
                        else if (!deriv_info.Base.Modifier.IsSealed)
                        {
                            ctx.AddError(ErrorCode.MissingOverrideModifier, deriv_info.Derived);
                        }

                        if (!deriv_info.Base.Modifier.IsSealed)
                        {
                            virtual_mapping.Update(deriv_info.Base, deriv_info.Derived);

                            // the rationale for keeping the same access level is this
                            // narrowing is pretty easy to skip by downcasting
                            // expanding looks like a good idea, but... -- the author of original type
                            // maybe had better understanding why given function is private/protected and not protected/public
                            // it is better to keep it safe, despite little annoyance (additional wrapper), than finding out
                            // how painful is violating that "good reason" because it was too easy to type "public"
                            if (!deriv_info.Base.Modifier.SameAccess(deriv_info.Derived.Modifier))
                                ctx.AddError(ErrorCode.AlteredAccessLevel, deriv_info.Derived.Modifier);
                        }
                        else if (deriv_info.Derived.Modifier.HasOverride)
                            ctx.AddError(ErrorCode.CannotOverrideSealedMethod, deriv_info.Derived);
                    }
                }
            }

            foreach (FunctionDefinition missing_impl in missing_func_implementations)
            {
                if (!isDerivedByAncestors(missing_impl))
                {
                    ctx.AddError(ErrorCode.BaseFunctionMissingImplementation, this, missing_impl);
                }
            }

            // here we eliminate "duplicate" entries -- if we have some method in ancestor A
            // and this method is overridden in ancestor B, then we would like to have
            // only method B listed, not both
            foreach (EntityInstance inherited in inherited_member_instances.ToArray())
            {
                IEntity target = inherited.Target;
                if (target is Property prop)
                    target = prop.Getter;
                if (target is FunctionDefinition func && isDerivedByAncestors(func))
                    inherited_member_instances.Remove(inherited);
            }

            this.InheritanceVirtualTable = virtual_mapping;
            this.DerivationTable = new DerivationTable(ctx, this, derivation_mapping);
            this.availableEntities = inherited_member_instances.Concat(this.NestedEntities().Select(it => it.InstanceOf)).StoreReadOnly();


            foreach (FunctionDefinition func in derivation_mapping.Where(it => !it.Value.Any()).Select(it => it.Key))
                ctx.AddError(ErrorCode.NothingToOverride, func);

            this.isEvaluated = true;
        }

        private bool isDerivedByAncestors(FunctionDefinition baseFunction)
        {
            foreach (EntityInstance ancestor in this.Inheritance.OrderedAncestorsWithoutObject)
            {
                if (ancestor.TargetType.InheritanceVirtualTable != null
                    && ancestor.TargetType.InheritanceVirtualTable.TryGetDerived(baseFunction, out FunctionDefinition dummy))
                {
                    return true;
                }
            }

            return false;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            if (this.Modifier.HasTrait)
            {
                if (!this.Name.Parameters.Any())
                    ctx.AddError(ErrorCode.NonGenericTrait, this);
                else
                {
                    if (!this.Constraints.Any())
                        ctx.AddError(ErrorCode.UnconstrainedTrait, this);

                    TypeDefinition host_type = AssociatedHost;
                    if (host_type == null)
                        ctx.AddError(ErrorCode.MissingHostTypeForTrait, this);
                }
            }

            if (this.Modifier.HasAssociatedReference)
            {
                if (!this.Modifier.IsSealed)
                    ctx.AddError(ErrorCode.AssociatedReferenceRequiresSealedType, this.Modifier);

                {
                    IEnumerable<FunctionDefinition> constructors = this.NestedFunctions.Where(it => it.IsInitConstructor());

                    foreach (FunctionDefinition cons in constructors.Skip(1))
                        ctx.AddError(ErrorCode.AssociatedReferenceRequiresSingleConstructor, cons);

                    FunctionDefinition primary_cons = constructors.First();
                    if (primary_cons.Parameters.Count != 1)
                        ctx.AddError(ErrorCode.AssociatedReferenceRequiresSingleParameter, primary_cons);
                    else
                    {
                        FunctionParameter cons_param = primary_cons.Parameters.Single();
                        if (!ctx.Env.IsReferenceOfType(cons_param.TypeName.Evaluation.Components))
                            ctx.AddError(ErrorCode.AssociatedReferenceRequiresReferenceParameter, cons_param.TypeName);
                        else if (cons_param.IsVariadic)
                            ctx.AddError(ErrorCode.AssociatedReferenceRequiresNonVariadicParameter, cons_param);
                        else if (cons_param.IsOptional)
                            ctx.AddError(ErrorCode.AssociatedReferenceRequiresNonOptionalParameter, cons_param);
                    }
                }

                {
                    IEnumerable<VariableDeclaration> ref_fields = this.NestedFields
                        .Where(it => ctx.Env.IsReferenceOfType(it.Evaluation.Components));

                    foreach (VariableDeclaration decl in ref_fields.Skip(1))
                        ctx.AddError(ErrorCode.AssociatedReferenceRequiresSingleReferenceField, decl);

                    VariableDeclaration primary = ref_fields.FirstOrDefault();
                    if (primary != null && primary.Modifier.HasReassignable)
                        ctx.AddError(ErrorCode.ReferenceFieldCannotBeReassignable, primary);
                }
            }

            foreach (NameReference parent in this.ParentNames.Skip(1))
                if (parent.Evaluation.Components.Cast<EntityInstance>().TargetType.IsTypeImplementation)
                    ctx.AddError(ErrorCode.TypeImplementationAsSecondaryParent, parent);

            {
                TypeDefinition primary_parent = this.Inheritance.GetTypeImplementationParent()?.TargetType;
                if (primary_parent != null)
                {
                    if (this.Modifier.HasEnum != primary_parent.Modifier.HasEnum)
                        ctx.AddError(ErrorCode.EnumCrossInheritance, this);

                    if (this.Modifier.HasTrait)
                        ctx.AddError(ErrorCode.TraitInheritingTypeImplementation, this.ParentNames.First());
                }
            }

            if (!this.Modifier.IsAbstract)
            {
                foreach (FunctionDefinition func in this.AllNestedFunctions)
                    if (func.Modifier.HasAbstract)
                        ctx.AddError(ErrorCode.NonAbstractTypeWithAbstractMethod, func);
                    else if (func.Modifier.HasBase && this.Modifier.IsSealed)
                        ctx.AddError(ErrorCode.SealedTypeWithBaseMethod, func);
            }

            {
                TypeMutability current_mutability = this.InstanceOf.MutabilityOfType(ctx);
                if (current_mutability != TypeMutability.Mutable)
                {
                    // the above check is more than checking just a flag
                    // for template types the mutability depends on parameter constraints
                    foreach (NameReference parent in this.ParentNames)
                    {
                        TypeMutability parent_mutability = parent.Evaluation.Components.MutabilityOfType(ctx);
                        if (parent_mutability == TypeMutability.Mutable)
                            ctx.AddError(ErrorCode.InheritanceMutabilityViolation, parent);
                    }
                }
            }

            if (!this.Modifier.HasMutable)
            {
                foreach (VariableDeclaration field in this.AllNestedFields)
                {
                    if (field.Modifier.HasReassignable)
                        ctx.AddError(ErrorCode.ReassignableFieldInImmutableType, field);
                    TypeMutability field_eval_mutability = field.Evaluation.Components.MutabilityOfType(ctx);
                    if (field_eval_mutability != TypeMutability.ConstAsSource
                        && field_eval_mutability != TypeMutability.GenericUnknownMutability
                        && field_eval_mutability != TypeMutability.Const)
                        ctx.AddError(ErrorCode.MutableFieldInImmutableType, field);
                }
                foreach (FunctionDefinition func in this.NestedFunctions
                    .Where(it => it.Modifier.HasMutable))
                {
                    ctx.AddError(ErrorCode.MutableFunctionInImmutableType, func);
                }

                foreach (Property property in this.NestedProperties
                    .Where(it => it.Setter != null))
                {
                    ctx.AddError(ErrorCode.PropertySetterInImmutableType, property);
                }
            }
        }

        private bool isHostOfTrait(TypeDefinition trait)
        {
            return trait.IsTrait && !this.IsTrait && EntityNameArityComparer.Instance.Equals(this.Name, trait.Name);
        }

        private void setupConstructors()
        {
            if (this.IsTemplateParameter || this.IsJoker || this.IsInterface || this.IsProtocol || this.IsTrait)
                return;

            if (!this.Modifier.HasNative && !this.NestedFunctions.Where(it => it.IsInitConstructor()).Any())
            {
                this.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Public, null, Block.CreateStatement()));
            }

            if (createZeroConstructor(isStatic: false))
                foreach (FunctionDefinition init_cons in this.NestedFunctions.Where(it => it.IsInitConstructor()))
                    init_cons.SetZeroConstructorCall();

            createZeroConstructor(isStatic: true);


#if USE_NEW_CONS
        createNewConstructors();
#endif
        }

        private bool createZeroConstructor(bool isStatic)
        {
            IEnumerable<VariableDeclaration> focus_fields = this.AllNestedFields
                .Where(it => it.Modifier.HasStatic == isStatic);

            IEnumerable<IExpression> field_defaults = focus_fields
                .Select(it => it.DetachFieldInitialization())
                .Where(it => it != null)
                .StoreReadOnly();

            // enum fields are special, we will get their initial values later
            if (!field_defaults.Any() && !focus_fields.Any(it => it.Modifier.HasEnum))
                return false;

            FunctionDefinition zero_constructor = FunctionDefinition.CreateZeroConstructor(
                isStatic ? EntityModifier.Static : EntityModifier.None,
                Block.CreateStatement(field_defaults));
            this.AddNode(zero_constructor);

            if (isStatic) // static constructor is used by runtime, not by user
                zero_constructor.SetIsMemberUsed();

            return true;
        }

#if USE_NEW_CONS
        private void createNewConstructors()
        {
            NameReference type_name = this.Name.CreateNameReference();

            if (!this.NestedFunctions.Any(it => it.IsNewConstructor()))
                foreach (FunctionDefinition init_cons in this.NestedFunctions.Where(it => it.IsInitConstructor()).StoreReadOnly())
                {
                    //if (this.NestedFunctions.Any(it => it.IsNewConstructor()
                    //      && it.Name.Arity == init_cons.Name.Arity && it.NOT_USED_CounterpartParameters(init_cons)))
                    //continue;

                    const string local_this = "__this__";

                    var new_cons = FunctionDefinition.CreateHeapConstructor(init_cons.Modifier, init_cons.Parameters,
                        type_name,
                        Block.CreateStatement(new IExpression[] {
                        VariableDeclaration.CreateStatement(local_this, null, Alloc.Create(type_name, useHeap: true)),
                        FunctionCall.Create(NameReference.Create(NameReference.Create(local_this), NameFactory.InitConstructorName),
                            init_cons.Parameters.Select(it => FunctionArgument.Create( it.Name.CreateNameReference())).ToArray()),
                        Return.Create(NameReference.Create(local_this))
                        }));

                    this.AddNode(new_cons);
                }
        }
#endif

        /*internal bool HasImplicitConversionConstructor(ComputationContext ctx, EntityInstance input)
{
   return this.NestedFunctions.Any(it => it.Name.Name == NameFactory.InitConstructorName
       && it.Name.Arity == 0
       && it.Modifier.HasImplicit
       && it.Parameters.Count == 1
       && input.MatchesTarget(ctx, it.Parameters.Single().TypeName.Evaluated(ctx),
       input.TargetType.AllowSlicedSubstitution) == TypeMatch.Pass);
}*/
        internal IEnumerable<FunctionDefinition> ImplicitInConverters()
        {
            return this.NestedFunctions
                .Where(it => it.IsInitConstructor()
                    && it.Name.Arity == 0
                    && it.Modifier.HasImplicit
                    && it.Parameters.Count == 1);
        }
        internal IEnumerable<FunctionDefinition> ImplicitOutConverters()
        {
            return this.NestedFunctions.Where(it => it.IsOutConverter() && it.Modifier.HasImplicit);
        }

        /*private void installNewConstructors()
        {
            foreach (FunctionDefinition init_cons in this.NestedFunctions.Where(it => it.Role == FunctionRole.InitConstructor))
            {
                if (!this.NestedFunctions.Any(it => it.Name.Name == NameFactory.NewConstructorName
                        && it.Name.Arity == init_cons.Name.Arity && it.NOT_USED_CounterpartParameters(init_cons)))
                {

                }
            }

        }*/


        private bool computeAncestors(ComputationContext ctx, HashSet<TypeDefinition> visited)
        {
            // we cannot use context visited, because it is only for direct use in Evaluated only
            if (this.IsInheritanceComputed)
                return true;
            if (!visited.Add(this))
                return false;

            var parents = new HashSet<EntityInstance>(ReferenceEqualityComparer<EntityInstance>.Instance);
            // distance to given ancestor, please note that we need to maximize distance to Object, since technically
            // object is parent of every type, so seeing "1" everytime is not productive
            var ancestors = new Dictionary<EntityInstance, int>(ReferenceEqualityComparer<EntityInstance>.Instance);

            IEnumerable<NameReference> all_parent_names = this.ParentNames;
            foreach (NameReference parent_name in all_parent_names)
            {
                parent_name.Surfed(ctx);
                EntityInstance parent = parent_name.Evaluation.Components as EntityInstance;
                if (parent == null)
                    continue;

                parents.Add(parent);

                if (!this.Modifier.HasHeapOnly && parent.Target.Modifier.HasHeapOnly)
                    ctx.AddError(ErrorCode.CrossInheritingHeapOnlyType, parent_name);

                if (parent.Target.Modifier.IsSealed)
                    ctx.AddError(ErrorCode.InheritingSealedType, parent_name);

                if (!parent.TargetType.computeAncestors(ctx, visited))
                    ctx.AddError(ErrorCode.CyclicTypeHierarchy, parent_name);
                else
                    parent.TargetType.Surfed(ctx);

                if (parent.TargetType.IsInheritanceComputed)
                    foreach (TypeAncestor ancestor in parent.TargetType.Inheritance.OrderedTypeAncestorsWithoutObject
                        .Select(it => new TypeAncestor(it.AncestorInstance.TranslateThrough(parent), it.Distance + 1)))
                    {
                        int dist;
                        if (ancestors.TryGetValue(ancestor.AncestorInstance, out dist))
                        {
                            if (ancestor.AncestorInstance == ctx.Env.IObjectType.InstanceOf)
                                ancestors[ancestor.AncestorInstance] = System.Math.Max(dist, ancestor.Distance);
                            else
                                ancestors[ancestor.AncestorInstance] = System.Math.Min(dist, ancestor.Distance);
                        }
                        else
                            ancestors.Add(ancestor.AncestorInstance, ancestor.Distance);
                    }
            }

            // now we have minimal parents
            parents.ExceptWith(ancestors.Keys);

            // almost the exactly the order user gave, however
            // user could give incorrect order in respect to interface/implementation 
            // so to to avoid further errors sort the parents in correct order
            List<EntityInstance> ordered_parents = all_parent_names //this.ParentNames
                .Select(it =>
                {
                    var p_instance = it.Evaluation.Components as EntityInstance;
                    return parents.Contains(p_instance) ? p_instance : null;
                })
                .Where(it => it != null)
                .Distinct() // https://stackoverflow.com/a/4734876/210342
                .OrderBy(it => it.IsTypeImplementation ? 0 : 1)
                .ToList();

            // add implicit Object only if it is not Object itself and if there are no given parents
            // when there are parents given we will get Object through parents
            if (this != ctx.Env.IObjectType && !parents.Any())
                ordered_parents.Add(ctx.Env.IObjectType.InstanceOf);

            foreach (EntityInstance parent in ordered_parents)
                ancestors.Add(parent, 1);

            int object_dist;
            if (ancestors.Any())
            {
                object_dist = ancestors.Max(it => it.Value);
                if (!ancestors.ContainsKey(ctx.Env.IObjectType.InstanceOf))
                    ++object_dist;
            }
            else if (this == ctx.Env.IObjectType)
                object_dist = 0;
            else
                throw new System.Exception("Having no ancestors is impossible");

            this.Inheritance = new TypeInheritance(new TypeAncestor(ctx.Env.IObjectType.InstanceOf, object_dist),
                ordered_parents,
                completeAncestors: ancestors.Select(it => new TypeAncestor(it.Key, it.Value)));

            return true;
        }
    }
}
