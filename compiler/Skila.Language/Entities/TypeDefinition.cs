using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Expressions;
using Skila.Language.Semantics;
using Skila.Language.Flow;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class TypeDefinition : TypeContainerDefinition
    {
        public static TypeDefinition Create(bool isPlain, EntityModifier modifier, NameDefinition name, bool allowSlicing,
            IEnumerable<NameReference> parents, IEnumerable<INode> features)
        {
            return new TypeDefinition(isPlain, modifier, allowSlicing, name, parents, features, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition Create(bool isPlain, EntityModifier modifier, NameDefinition name, bool allowSlicing,
            IEnumerable<NameReference> parents = null)
        {
            return new TypeDefinition(isPlain, modifier, allowSlicing, name, parents, null, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition Create(EntityModifier modifier, NameDefinition name, bool allowSlicing,
            IEnumerable<NameReference> parents = null, IEnumerable<IEntity> entities = null)
        {
            return new TypeDefinition(false, modifier, allowSlicing, name, parents, entities, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition CreateValue(EntityModifier modifier, NameDefinition name, IEnumerable<NameReference> parents = null)
        {
            return new TypeDefinition(false, modifier, false, name, parents, null, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition CreateProtocol(EntityModifier modifier, NameDefinition name, IEnumerable<NameReference> parents = null)
        {
            return new TypeDefinition(false, modifier | EntityModifier.Protocol, false, name, parents, null, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition CreateHeapOnly(EntityModifier modifier, NameDefinition name, IEnumerable<NameReference> parents = null)
        {
            return new TypeDefinition(false, modifier | EntityModifier.HeapOnly, true, name, parents, null, functorSignature: null, typeParameter: null);
        }
        public static TypeDefinition CreateFunctor(NameDefinition name, IFunctionSignature signature)
        {
            return new TypeDefinition(false, EntityModifier.Const, false, name, new[] { NameFactory.ObjectTypeReference() }, null,
                signature, typeParameter: null);
        }

        // used for creating embedded type definitions of type parameters, e.g. Tuple<T1,T2>, here we create T1 and T2
        public static TypeDefinition CreateTypeParameter(EntityModifier modifier, NameDefinition name, IEnumerable<NameReference> parents,
            TemplateParameter typeParameter)
        {
            return new TypeDefinition(false, modifier, false, name, parents, null, null, typeParameter);
        }


        public static TypeDefinition Joker { get; } = TypeDefinition.Create(EntityModifier.None, NameDefinition.Create(NameFactory.JokerTypeName), allowSlicing: true);

        public bool IsTypeImplementation => !this.IsProtocol;
        public bool IsProtocol => this.Modifier.HasProtocol;
        public bool IsJoker => this.Name.Name == NameFactory.JokerTypeName;

        public TemplateParameter TemplateParameter { get; }
        public bool IsTemplateParameter => this.TemplateParameter != null;
        public IReadOnlyCollection<NameReference> ParentNames { get; }
        //public IEnumerable<EntityInstance> ParentTypes => this.ParentNames.Select(it => it.Binding.Match).WhereType<EntityInstance>();
        public Inheritance Inheritance { get; private set; }
        private bool inheritanceComputed => this.Inheritance != null;
        public IFunctionSignature FunctorSignature { get; }

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(this.FunctorSignature)
            .Concat(this.ParentNames)
            .Where(it => it != null);

        public bool AllowSlicedSubstitution { get; }

        public bool IsPlain { get; }

        private TypeDefinition(bool isPlain,
            EntityModifier modifier,
            bool allowSlicing,
            NameDefinition name,
            IEnumerable<NameReference> parents,
            IEnumerable<INode> features,
            IFunctionSignature functorSignature,
            TemplateParameter typeParameter) : base(modifier, name)
        {
            this.IsPlain = isPlain;
            this.AllowSlicedSubstitution = allowSlicing;
            this.TemplateParameter = typeParameter;
            this.FunctorSignature = functorSignature;
            this.ParentNames = (parents ?? Enumerable.Empty<NameReference>()).StoreReadOnly();

            features?.ForEach(it => AddNode(it));
            addAutoConstructors();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            constructionCompleted = true;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            if (!IsComputed)
            {
                IsComputed = true;

                base.Evaluate(ctx);

                if (this.DebugId.Id == 286)
                {
                    ;
                }

                computeAncestors(ctx, new HashSet<TypeDefinition>());


                foreach (NameReference parent in this.ParentNames.Skip(1))
                    if (parent.Evaluation.Cast<EntityInstance>().Target.CastType().IsTypeImplementation)
                        ctx.AddError(ErrorCode.TypeImplementationAsSecondaryParent, parent);

                if (this.Modifier.HasConst)
                {
                    foreach (NameReference parent in this.ParentNames)
                        if (!parent.Evaluation.IsImmutableType(ctx))
                            ctx.AddError(ErrorCode.ImmutableInheritsMutable, this);

                    foreach (VariableDeclaration field in this.AllNestedFields)
                    {
                        if (field.Modifier.HasReassignable)
                            ctx.AddError(ErrorCode.ReassignableFieldInImmutableType, field);
                        if (!field.Evaluated(ctx).IsImmutableType(ctx))
                            ctx.AddError(ErrorCode.MutableFieldInImmutableType, field);
                    }
                }
            }
        }

        private void addAutoConstructors()
        {
            if (this.IsJoker || this.IsProtocol)
                return;

            if (this.DebugId.Id == 1564)
            {
                ;
            }
            {
                IEnumerable<IExpression> field_defaults = this.AllNestedFields
                    .Select(it => it.DetachFieldInitialization())
                    .Where(it => it != null);
                this.AddNode(FunctionDefinition.CreateZeroConstructor(Block.CreateStatement(field_defaults)));
            }

            if (!this.NestedFunctions.Any(it => it.IsInitConstructor()))
            {
                this.AddNode(FunctionDefinition.CreateInitConstructor(EntityModifier.Public, null, null));
            }

#if USE_NEW_CONS
        createNewConstructors();
#endif
        }

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

        /*private static FunctionDefinition installNewConstructor(TypeDefinition type, FunctionDefinition initConstructor)
{
    var func_params = initConstructor.FunctionParams.Clone(withBindings: true, withInits: true, asExtension: false);
    func_params.Elements.ForEach(it => it.ResetUsageRequirement(ValueUsageEnum.CanBeIgnored));

    List<INode> instructions = null;

    {
        instructions = new List<INode>();

        const string local_this = "__this__";
        var var_decl = VariableDeclaration.CreateStatement(local_this, type.InstanceOf.NameOf, Undef.Create());
        var init_call = FunctionCall.Create(NameReference.Create(NameReference.Create(local_this),NameFactory.InitConstructorName));
        var ret = Return.Create(NameReference.Create(local_this));

        instructions.Add(var_decl);
        instructions.Add(init_call);
        instructions.Add(ret);
    }

    var cons =  FunctionDefinition.CreateNewConstructor(EntityModifier.Static,TreeNode.DO_NOT_USE_NoCoords, null, true,
                            EntityModifier.None()
                            .SetStatic(true)
                            // important piece -- new constructor for interface has to be implemented in EVERY descendant class, not just child class
                            .SetPinned(type.IsInterface() || initConstructor.ModifierGiven.HasPinnedFlag)
                            .SetRefines(initConstructor.ModifierGiven.HasRefinesFlag)
                            .SetLeastVisbileFlag(initConstructor.ModifierComputed)
                            ,

                            TemplatedIdentifier.Create(TreeConstants.NewConstructor()),
                          func_params,
                          new FunctionOutcome(TreeNode.DO_NOT_USE_NoCoords,
                            ValueUsageEnum.UseRequired,
                          EntitySelector.CreateContextSelf(TreeNode.DO_NOT_USE_NoCoords)),
                          GenericConstraint.NoConstraints,
                           Function.FuncPurposeEnum.None);


    if (instructions != null)
        cons.SetUserCodeAsBlock(instructions);

    type.AddNode(cons);

    return cons;
}*/

        /*internal bool HasImplicitConversionConstructor(ComputationContext ctx, EntityInstance input)
{
   return this.NestedFunctions.Any(it => it.Name.Name == NameFactory.InitConstructorName
       && it.Name.Arity == 0
       && it.Modifier.HasImplicit
       && it.Parameters.Count == 1
       && input.MatchesTarget(ctx, it.Parameters.Single().TypeName.Evaluated(ctx),
       input.Target.CastType().AllowSlicedSubstitution) == TypeMatch.Pass);
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

                if (parent.Target.Modifier.HasFinal)
                    ctx.AddError(ErrorCode.InheritingFinalType, parent_name);

                if (!parent.TargetType.computeAncestors(ctx, visited))
                    ctx.AddError(ErrorCode.CyclicTypeHierarchy, parent_name);

                if (parent.TargetType.inheritanceComputed)
                    parent.TargetType.Inheritance.AncestorsWithoutObject.Select(it => it.TranslateThrough(parent))
                        .ForEach(it => ancestors.Add(it));
            }

            // add implicit Object only if it is not Object itself and if there are no given parents
            // when there are parents given we will get Object through parents
            if (this != ctx.Env.ObjectType && !parents.Any())
                parents.Add(ctx.Env.ObjectType.InstanceOf);

            // now we have minimal parents
            parents.ExceptWith(ancestors);
            this.Inheritance = new Inheritance(ctx.Env.ObjectType.InstanceOf,
                parents, ancestors.Concat(parents));

            return true;
        }
    }
}
