using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;
using System;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeDefinition : TypeContainerDefinition
    {
        public static TypeDefinition Create(bool isPlain, EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints, bool allowSlicing,
            IEnumerable<NameReference> parents, IEnumerable<INode> features)
        {
            return new TypeDefinition(isPlain, modifier, allowSlicing, name, constraints, parents, features,
                typeParameter: null);
        }
        public static TypeDefinition Create(bool isPlain, EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            bool allowSlicing,
            IEnumerable<NameReference> parents = null)
        {
            return new TypeDefinition(isPlain, modifier, allowSlicing, name, constraints, parents, null,
                typeParameter: null);
        }
        public static TypeDefinition Create(EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            bool allowSlicing,
            IEnumerable<NameReference> parents = null, IEnumerable<IEntity> entities = null)
        {
            return new TypeDefinition(false, modifier, allowSlicing, name, constraints,
                parents, entities,
                typeParameter: null);
        }
        public static TypeDefinition CreateFunctionInterface(NameDefinition name)
        {
            return new TypeDefinition(false,
                EntityModifier.Interface,
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
            return new TypeDefinition(false, modifier, false, NameDefinition.Create(typeParameter.Name), null,
                typeParameter.Constraint.InheritsNames, typeParameter.Constraint.Functions,
                typeParameter);
        }


        public static TypeDefinition Joker { get; } = TypeDefinition.Create(EntityModifier.None,
            NameDefinition.Create(NameFactory.JokerTypeName), null, allowSlicing: true);

        public bool IsTypeImplementation => !this.IsInterface && !this.IsProtocol;
        public bool IsInterface => this.Modifier.HasInterface;
        public bool IsProtocol => this.Modifier.HasProtocol;
        public bool IsAbstract => this.IsInterface || this.Modifier.HasAbstract || this.IsProtocol;

        public bool IsJoker => this.Name.Name == NameFactory.JokerTypeName;

        public TemplateParameter TemplateParameter { get; }
        public bool IsTemplateParameter => this.TemplateParameter != null;
        public IReadOnlyCollection<NameReference> ParentNames { get; }
        //public IEnumerable<EntityInstance> ParentTypes => this.ParentNames.Select(it => it.Binding.Match).WhereType<EntityInstance>();
        public TypeInheritance Inheritance { get; private set; }
        private bool inheritanceComputed => this.Inheritance != null;

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(this.ParentNames)
            .Where(it => it != null);

        public bool AllowSlicedSubstitution { get; }

        public bool IsPlain { get; }

        public VirtualTable InheritanceVirtualTable { get; private set; }

        private readonly FunctionDefinition zeroConstructor;
        public FunctionDefinition ZeroConstructor => this.zeroConstructor;

        // public FunctionDefinition InvokeLambda => this.NestedFunctions.Single(it => it.Name.Name == NameFactory.LambdaInvoke);

        private TypeDefinition(bool isPlain,
            EntityModifier modifier,
            bool allowSlicing,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<NameReference> parents,
            IEnumerable<INode> features,
            TemplateParameter typeParameter) : base(modifier, name, constraints)
        {
            this.IsPlain = isPlain;
            this.AllowSlicedSubstitution = allowSlicing;
            this.TemplateParameter = typeParameter;
            this.ParentNames = (parents ?? Enumerable.Empty<NameReference>()).StoreReadOnly();

            features?.ForEach(it => AddNode(it));
            addAutoConstructors(ref this.zeroConstructor);

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            constructionCompleted = true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (IsComputed)
                return;

            IsComputed = true;

            base.Evaluate(ctx);

            if (this.DebugId.Id == 3945)
            {
                ;
            }

            computeAncestors(ctx, new HashSet<TypeDefinition>());


            foreach (NameReference parent in this.ParentNames.Skip(1))
                if (parent.Evaluation.Components.Cast<EntityInstance>().TargetType.IsTypeImplementation)
                    ctx.AddError(ErrorCode.TypeImplementationAsSecondaryParent, parent);


            {
                // base method -> derived (here) method
                var derivations = new Dictionary<FunctionDefinition, FunctionDefinition>();
                HashSet<FunctionDefinition> current_functions = this.NestedFunctions.ToHashSet();
                foreach (EntityInstance ancestor in this.Inheritance.AncestorsWithoutObject)
                {
                    foreach (FunctionDerivation deriv_info in TypeDefinitionExtension.PairDerivations(ctx, ancestor, current_functions))
                    {
                        if (deriv_info.Derived == null)
                        {
                            if (deriv_info.Base.IsAbstract)
                                ctx.AddError(ErrorCode.MissingFunctionImplementation, this, deriv_info.Base);
                        }
                        else
                        {
                            bool removed = current_functions.Remove(deriv_info.Derived);

                            if (!deriv_info.Derived.Modifier.HasRefines)
                                ctx.AddError(ErrorCode.MissingDerivedModifier, deriv_info.Derived);

                            if (!deriv_info.Base.IsSealed)
                            {
                                if (!removed)
                                    throw new System.Exception("Internal error");
                                derivations.Add(deriv_info.Base, deriv_info.Derived);

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

                this.InheritanceVirtualTable = new VirtualTable(derivations, isPartial: false);

                foreach (FunctionDefinition func in current_functions.Where(it => it.Modifier.HasRefines)) 
                    ctx.AddError(ErrorCode.NothingToDerive, func);
            }

            if (!this.IsAbstract)
            {
                foreach (FunctionDefinition func in this.NestedFunctions)
                    if (func.IsAbstract)
                        ctx.AddError(ErrorCode.NonAbstractTypeWithAbstractMethod, func);
                    else if (func.Modifier.HasBase && this.Modifier.HasSealed)
                        ctx.AddError(ErrorCode.SealedTypeWithBaseMethod, func);
            }

            if (this.Modifier.HasImmutable)
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

        private void addAutoConstructors(ref FunctionDefinition zeroConstructor)
        {
            if (this.IsTemplateParameter || this.IsJoker || this.IsInterface || this.IsProtocol)
                return;

            if (this.DebugId.Id == 1564)
            {
                ;
            }
            {
                IEnumerable<IExpression> field_defaults = this.AllNestedFields
                    .Select(it => it.DetachFieldInitialization())
                    .Where(it => it != null).StoreReadOnly();
                if (field_defaults.Any())
                {
                    zeroConstructor = FunctionDefinition.CreateZeroConstructor(Block.CreateStatement(field_defaults));
                    this.AddNode(zeroConstructor);
                }
            }

            if (!this.NestedFunctions.Any(it => it.IsInitConstructor()))
            {
                this.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Public, null, Block.CreateStatement()));
            }

#if USE_NEW_CONS
        createNewConstructors();
#endif
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
            if (this.inheritanceComputed)
                return true;
            if (!visited.Add(this))
                return false;

            var parents = new HashSet<EntityInstance>(ReferenceEqualityComparer<EntityInstance>.Instance);
            var ancestors = new HashSet<EntityInstance>(ReferenceEqualityComparer<EntityInstance>.Instance);

            foreach (NameReference parent_name in this.ParentNames)
            {
                EntityInstance parent = parent_name.Evaluated(ctx) as EntityInstance;
                if (parent == null)
                    continue;

                parents.Add(parent);

                if (!this.Modifier.HasHeapOnly && parent.Target.Modifier.HasHeapOnly)
                    ctx.AddError(ErrorCode.CrossInheritingHeapOnlyType, parent_name);

                if (parent.Target.Modifier.HasSealed)
                    ctx.AddError(ErrorCode.InheritingSealedType, parent_name);

                if (!parent.TargetType.computeAncestors(ctx, visited))
                    ctx.AddError(ErrorCode.CyclicTypeHierarchy, parent_name);

                if (parent.TargetType.inheritanceComputed)
                    parent.TargetType.Inheritance.AncestorsWithoutObject.Select(it => it.TranslateThrough(parent))
                        .ForEach(it => ancestors.Add(it));
            }

            // now we have minimal parents
            parents.ExceptWith(ancestors);

            if (this.DebugId.Id == 85)
            {
                ;
            }

            // add implicit Object only if it is not Object itself and if there are no given parents
            // when there are parents given we will get Object through parents
            // also exclude Void, this is separate type from entire hierarchy
            if (this != ctx.Env.ObjectType && !parents.Any() && this != ctx.Env.VoidType)
                parents.Add(ctx.Env.ObjectType.InstanceOf);

            this.Inheritance = new TypeInheritance(ctx.Env.ObjectType.InstanceOf,
                parents, completeAncestors: ancestors.Concat(parents));

            return true;
        }
    }
}
