using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;

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
                typeParameter: null);
        }
        public static TypeDefinition CreateFunctionInterface(NameDefinition name)
        {
            return new TypeDefinition(EntityModifier.Interface,
                false, name, null, new[] { NameFactory.ObjectTypeReference() },
                features: null,
                typeParameter: null);
        }

        // used for creating embedded type definitions of type parameters, e.g. Tuple<T1,T2>, here we create T1 and T2
        public static TypeDefinition CreateTypeParameter(TemplateParameter typeParameter)
        {
            EntityModifier modifier = typeParameter.Constraint.Modifier;
            if (typeParameter.Constraint.Functions.Any())
                modifier |= EntityModifier.Protocol;
            else
                modifier |= EntityModifier.Base;
            if (!modifier.HasConst)
                modifier |= EntityModifier.Mutable;
            return new TypeDefinition(modifier, false, NameDefinition.Create(typeParameter.Name), null,
                typeParameter.Constraint.InheritsNames, typeParameter.Constraint.Functions,
                typeParameter);
        }


        public static TypeDefinition Joker { get; } = TypeDefinition.Create(EntityModifier.None,
            NameDefinition.Create(NameFactory.JokerTypeName), null, allowSlicing: true);

        public bool IsTypeImplementation => !this.IsInterface && !this.IsProtocol;
        public bool IsInterface => this.Modifier.HasInterface;
        public bool IsProtocol => this.Modifier.HasProtocol;

        public bool IsJoker => this.Name.Name == NameFactory.JokerTypeName;

        public TemplateParameter TemplateParameter { get; }
        public bool IsTemplateParameter => this.TemplateParameter != null;
        public IReadOnlyCollection<NameReference> ParentNames { get; }
        //public IEnumerable<EntityInstance> ParentTypes => this.ParentNames.Select(it => it.Binding.Match).WhereType<EntityInstance>();
        public TypeInheritance Inheritance { get; private set; }
        public bool IsInheritanceComputed => this.Inheritance != null;

        private bool isEvaluated;
        public override IEnumerable<ISurfable> Surfables => base.Surfables.Concat(this.ParentNames);

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(this.ParentNames)
            .Where(it => it != null);

        public bool AllowSlicedSubstitution { get; }

        public VirtualTable InheritanceVirtualTable { get; private set; }
        public DerivationTable DerivationTable { get; private set; }

        private IEnumerable<IEntity> availableEntities;
        public override IEnumerable<IEntity> AvailableEntities => this.availableEntities;

        private TypeDefinition(EntityModifier modifier,
            bool allowSlicing,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<NameReference> parents,
            IEnumerable<INode> features,
            TemplateParameter typeParameter) : base(modifier, name, constraints)
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

            compute(ctx);
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (!this.isEvaluated)
                compute(ctx);

            IsComputed = true;
        }

        private void compute(ComputationContext ctx)
        {
            computeAncestors(ctx, new HashSet<TypeDefinition>());


            // base method -> derived (here) method
            var virtual_mapping = new Dictionary<FunctionDefinition, FunctionDefinition>();
            // derived (here) method -> base methods
            Dictionary<FunctionDefinition, List<FunctionDefinition>> derivation_mapping = this.NestedFunctions
                .Where(it => it.Modifier.HasRefines)
                .ToDictionary(it => it, it => new List<FunctionDefinition>());

            HashSet<IEntity> inherited_entities = this.Inheritance.MinimalParentsWithoutObject
                .SelectMany(it => it.TargetType.AvailableEntities ?? Enumerable.Empty<IEntity>())
                .Where(it => it.Modifier.HasPublic || it.Modifier.HasProtected)
                .Where(it => !(it is FunctionDefinition func) || !func.IsConstructor())
                .ToHashSet();

            var missing_func_implementations = new List<FunctionDefinition>();

            foreach (EntityInstance ancestor in this.Inheritance.AncestorsWithoutObject)
            {
                foreach (FunctionDerivation deriv_info in TypeDefinitionExtension.PairDerivations(ctx, ancestor, this.NestedFunctions))
                {
                    if (deriv_info.Derived == null)
                    {
                        // we can skip implementation or abstract signature of the function in the abstract type
                        if (deriv_info.Base.Modifier.HasAbstract && !this.Modifier.IsAbstract)
                            missing_func_implementations.Add(deriv_info.Base);
                    }
                    else
                    {
                        inherited_entities.Remove(deriv_info.Base);

                        if (!deriv_info.Derived.Modifier.HasRefines)
                            ctx.AddError(ErrorCode.MissingDerivedModifier, deriv_info.Derived);
                        else
                            derivation_mapping[deriv_info.Derived].Add(deriv_info.Base);

                        if (!deriv_info.Base.Modifier.IsSealed)
                        {
                            virtual_mapping.Add(deriv_info.Base, deriv_info.Derived);

                            // the rationale for keeping the same access level is this
                            // narrowing is pretty easy to skip by downcasting
                            // expanding looks like a good idea, but... -- the author of original type
                            // maybe had better understanding why given function is private/protected and not protected/public
                            // it is better to keep it safe, despite little annoyance (additional wrapper), than finding out
                            // how painful is violating that "good reason" because it was too easy to type "public"
                            if (!deriv_info.Base.Modifier.SameAccess(deriv_info.Derived.Modifier))
                                ctx.AddError(ErrorCode.AlteredAccessLevel, deriv_info.Derived.Modifier);
                        }
                        else if (deriv_info.Derived.Modifier.HasRefines)
                            ctx.AddError(ErrorCode.CannotDeriveSealedMethod, deriv_info.Derived);
                    }
                }
            }

            foreach (FunctionDefinition missing_impl in missing_func_implementations)
            {
                bool found = false;
                foreach (EntityInstance ancestor in this.Inheritance.AncestorsWithoutObject)
                {

                    if (ancestor.TargetType.InheritanceVirtualTable.TryGetDerived(missing_impl, out FunctionDefinition dummy))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    ctx.AddError(ErrorCode.MissingFunctionImplementation, this, missing_impl);
            }

            this.InheritanceVirtualTable = new VirtualTable(virtual_mapping, isPartial: false);
            this.DerivationTable = new DerivationTable(ctx, derivation_mapping);
            this.availableEntities = inherited_entities.Concat(this.NestedEntities()).StoreReadOnly();

            foreach (FunctionDefinition func in derivation_mapping.Where(it => !it.Value.Any()).Select(it => it.Key))
                ctx.AddError(ErrorCode.NothingToDerive, func);

            this.isEvaluated = true;
        }

        public override void Validate(ComputationContext ctx)
        {
            base.Validate(ctx);

            foreach (NameReference parent in this.ParentNames.Skip(1))
                if (parent.Evaluation.Components.Cast<EntityInstance>().TargetType.IsTypeImplementation)
                    ctx.AddError(ErrorCode.TypeImplementationAsSecondaryParent, parent);

            {
                TypeDefinition primary_parent = this.Inheritance.GetTypeImplementationParent()?.TargetType;
                if (primary_parent != null && this.Modifier.HasEnum != primary_parent.Modifier.HasEnum)
                    ctx.AddError(ErrorCode.EnumCrossInheritance, this);
            }

            if (!this.Modifier.IsAbstract)
            {
                foreach (FunctionDefinition func in this.NestedFunctions)
                    if (func.Modifier.HasAbstract)
                        ctx.AddError(ErrorCode.NonAbstractTypeWithAbstractMethod, func);
                    else if (func.Modifier.HasBase && this.Modifier.IsSealed)
                        ctx.AddError(ErrorCode.SealedTypeWithBaseMethod, func);
            }

            if (this.Modifier.IsImmutable)
            {
                foreach (NameReference parent in this.ParentNames)
                    if (!parent.Evaluation.Components.IsImmutableType(ctx))
                        ctx.AddError(ErrorCode.ImmutableInheritsMutable, parent);

                foreach (VariableDeclaration field in this.AllNestedFields)
                {
                    if (field.Modifier.HasReassignable)
                        ctx.AddError(ErrorCode.ReassignableFieldInImmutableType, field);
                    if (!field.Evaluated(ctx).IsImmutableType(ctx))
                        ctx.AddError(ErrorCode.MutableFieldInImmutableType, field);
                }
            }
        }

        private void setupConstructors()
        {
            if (this.DebugId.Id == 3181)
            {
                ;
            }
            if (this.IsTemplateParameter || this.IsJoker || this.IsInterface || this.IsProtocol)
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
            IEnumerable<IExpression> field_defaults = this.AllNestedFields
                .Where(it => it.Modifier.HasStatic == isStatic)
                .Select(it => it.DetachFieldInitialization())
                .Where(it => it != null)
                .StoreReadOnly();

            if (!field_defaults.Any())
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
            if (this.DebugId.Id == 85)
            {
                ;
            }
            // we cannot use context visited, because it is only for direct use in Evaluated only
            if (this.IsInheritanceComputed)
                return true;
            if (!visited.Add(this))
                return false;

            var parents = new HashSet<EntityInstance>(ReferenceEqualityComparer<EntityInstance>.Instance);
            var ancestors = new HashSet<EntityInstance>(ReferenceEqualityComparer<EntityInstance>.Instance);

            foreach (NameReference parent_name in this.ParentNames)
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

                if (parent.TargetType.IsInheritanceComputed)
                    parent.TargetType.Inheritance.AncestorsWithoutObject.Select(it => it.TranslateThrough(parent))
                        .ForEach(it => ancestors.Add(it));
            }

            // now we have minimal parents
            parents.ExceptWith(ancestors);

            if (this.DebugId.Id == 85)
            {
                ;
            }

            // almost the exactly the order user gave, however
            // user could give incorrect order in respect to interface/implementation 
            // so to to avoid further errors sort the parents in correct order
            List<EntityInstance> ordered_parents = this.ParentNames
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
            // also exclude Void, this is separate type from entire hierarchy
            if (this != ctx.Env.ObjectType && !parents.Any() && this != ctx.Env.VoidType)
                ordered_parents.Add(ctx.Env.ObjectType.InstanceOf);

            this.Inheritance = new TypeInheritance(ctx.Env.ObjectType.InstanceOf,
                ordered_parents, completeAncestors: ordered_parents.Concat(ancestors));

            return true;
        }
    }
}
