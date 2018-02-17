using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using System.Linq;
using Skila.Language.Extensions;
using System;
using Skila.Language.Semantics;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionDefinition : TemplateDefinition, IEntity, IExecutableScope, IMember
    {
        public static FunctionDefinition CreateFunction(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            Block body)
        {
            return new FunctionDefinition(modifier,
                name, constraints, parameters, callMode, result, constructorChainCall: null, body: body);
        }
        public static FunctionDefinition CreateFunction(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            FunctionCall constructorChainCall,
            Block body)
        {
            return new FunctionDefinition(modifier,
                name, constraints, parameters, callMode, result, constructorChainCall, body);
        }

        internal void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public static FunctionDefinition CreateInitConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            Block body,
            FunctionCall constructorChainCall = null)
        {
            return new FunctionDefinition(modifier,
                                NameFactory.InitConstructorNameDefinition(), null,
                                parameters,
                                ExpressionReadMode.OptionalUse,
                                NameFactory.UnitTypeReference(),
                                constructorChainCall, body: body);
        }

        public static FunctionDefinition CreateHeapConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            NameReference typeName,
            Block body)
        {
            return new FunctionDefinition(
                modifier | EntityModifier.Static,
                NameFactory.NewConstructorNameDefinition(), null,
                parameters, ExpressionReadMode.ReadRequired, NameFactory.PointerTypeReference(typeName),
                constructorChainCall: null, body: body);
        }
        public static FunctionDefinition CreateZeroConstructor(EntityModifier modifier, Block body)
        {
            return new FunctionDefinition(modifier | EntityModifier.Private,
                                NameFactory.ZeroConstructorNameDefinition(), null,
                                null,
                                ExpressionReadMode.OptionalUse,
                                NameFactory.UnitTypeReference(),
                                constructorChainCall: null, body: body);
        }

        public bool IsResultTypeNameInfered { get; }
        public INameReference ResultTypeName { get; private set; }
        private readonly List<IEntityInstance> resultTypeCandidates;
        public Block UserBody { get; }
        public IReadOnlyList<FunctionParameter> Parameters { get; }
        public FunctionParameter MetaThisParameter { get; private set; }
        private NameReference thisNameReference;
        public ExpressionReadMode CallMode { get; private set; }
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(UserBody);

        internal LambdaTrap LambdaTrap { get; set; }

        public bool IsMemberUsed { get; private set; }
        public override IEnumerable<EntityInstance> AvailableEntities => this.NestedEntityInstances();

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            // parameters have to go before user body, so they are registered for use
            .Concat(this.Parameters)
            .Concat(this.MetaThisParameter)
            .Concat(UserBody)
            .Concat(this.ResultTypeName)
            .Concat(this.thisNameReference)
            .Where(it => it != null);

        public bool IsDeclaration => this.UserBody == null;

        public bool IsLambdaInvoker => this.Name.Name == NameFactory.LambdaInvoke;
        public bool IsLambda => this.EnclosingScope<TemplateDefinition>().IsFunction();

        private FunctionDefinition(EntityModifier modifier,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            FunctionCall constructorChainCall,
            Block body)
            : base(modifier | (body == null ? EntityModifier.Abstract : EntityModifier.None), name, constraints)
        {
            parameters = parameters ?? Enumerable.Empty<FunctionParameter>();

            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.ResultTypeName = result;
            this.IsResultTypeNameInfered = result == null;
            if (this.IsResultTypeNameInfered)
                this.resultTypeCandidates = new List<IEntityInstance>();
            this.UserBody = body;
            this.CallMode = callMode;

            // attaching zero-constructor call to the body of the function will be done when attaching entire function to a type
            if (constructorChainCall != null)
                this.UserBody.SetConstructorChainCall(constructorChainCall);

            if (this.IsLambdaInvoker)
                this.LambdaTrap = new LambdaTrap();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.constructionCompleted = true;
        }

        public NameReference CreateFunctionInterface()
        {
            return NameFactory.IFunctionTypeReference(this.Parameters.Select(it => it.ElementTypeName)
                .Concat(this.ResultTypeName).ToArray());
        }

        internal void AddResultTypeCandidate(INameReference typenameCandidate)
        {
            this.resultTypeCandidates.Add(typenameCandidate.Evaluation.Components);
        }

        internal void InferResultType(ComputationContext ctx)
        {
            if (!this.resultTypeCandidates.Any()) // no returns
                this.ResultTypeName = ctx.Env.UnitType.InstanceOf.NameOf;
            else
            {
                IEntityInstance common = this.resultTypeCandidates.First();
                foreach (IEntityInstance candidate in this.resultTypeCandidates.Skip(1))
                {
                    if (!TypeMatcher.LowestCommonAncestor(ctx, common, candidate, out common))
                    {
                        ctx.AddError(ErrorCode.CannotInferResultType, this);
                        this.ResultTypeName = EntityInstance.Joker.NameOf;
                        return;
                    }
                }

                foreach (IEntityInstance candidate in this.resultTypeCandidates)
                {
                    // it is tempting to allowing conversions here, but it would mean that we have back to all "returns"
                    // to apply such conversions, besides such fluent result type is a bit of a stretch
                    TypeMatch match = candidate.MatchesTarget(ctx, common, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false));
                    if (match != TypeMatch.Same && match != TypeMatch.Substitute)
                    {
                        ctx.AddError(ErrorCode.CannotInferResultType, this);
                        this.ResultTypeName = EntityInstance.Joker.NameOf;
                        return;
                    }
                }

                this.ResultTypeName = common.NameOf;
            }
        }


        public override bool AttachTo(INode parent)
        {
            // IMPORTANT: when function is attached to a type, the type is NOT fully constructed!

            if (this.DebugId.Id == 3058)
            {
                ;
            }

            bool result = base.AttachTo(parent);

            // property accessors will see owner type in second attach, when property is attached to type
            TypeDefinition owner_type = this.ContainingType();
            if (this.MetaThisParameter == null && owner_type != null // method
                && !this.Modifier.HasStatic && !owner_type.Modifier.HasStatic)
            {
                NameReference type_name = owner_type.InstanceOf.NameOf; // initially already tied to target
                this.MetaThisParameter = FunctionParameter.Create(NameFactory.ThisVariableName,
                    this.Modifier.HasHeapOnly ? NameFactory.PointerTypeReference(type_name) : NameFactory.ReferenceTypeReference(type_name),
                    Variadic.None, null, isNameRequired: false,
                    usageMode: ExpressionReadMode.OptionalUse);
                this.MetaThisParameter.AttachTo(this);
            }

            // marking regular functions a static one will make further processing easier
            if (!this.Modifier.HasStatic &&  parent is IEntity entity_parent && entity_parent.Modifier.HasStatic) 
                this.SetModifier(this.Modifier | EntityModifier.Static);

            if (!this.Modifier.IsAccessSet)
            {
                if (parent is TypeContainerDefinition)
                    this.SetModifier(this.Modifier | EntityModifier.Public);
                else if (parent is Property prop)
                    this.SetModifier(this.Modifier | prop.Modifier.AccessLevels);
            }

            return result;
        }

        public void SetZeroConstructorCall()
        {
            if (this.UserBody.constructorChainCall == null || this.UserBody.constructorChainCall.Name.IsBaseInitReference)
            {
                FunctionCall zero_call = FunctionCall.Constructor(NameReference.Create(NameFactory.ThisVariableName, NameFactory.ZeroConstructorName));
                this.UserBody.SetZeroConstructorCall(zero_call);
            }
        }

        public override string ToString()
        {
            string s = "";
            TypeDefinition type_owner = this.ContainingType();
            if (type_owner != null)
                s += $"{type_owner}::";
            s += $"{(base.ToString())}(" + this.Parameters.Select(it => it.ToString()).Join(",") + $") -> {this.ResultTypeName}";
            return s;
        }

        internal NameReference GetThisNameReference()
        {
            if (this.thisNameReference == null)
            {
                this.thisNameReference = NameReference.Create(null, NameFactory.ThisVariableName, null,
                    //this,
                    this.MetaThisParameter.InstanceOf);
                this.thisNameReference.AttachTo(this);
            }

            return this.thisNameReference;
        }

        public override void Evaluate(ComputationContext ctx)
        {
            base.Evaluate(ctx);

            this.IsComputed = true;
        }

        public override void Validate(ComputationContext ctx)
        {
            TypeDefinition type_owner = this.ContainingType();

            if (type_owner != null && type_owner.IsTrait && this.IsAnyConstructor())
                ctx.AddError(ErrorCode.TraitConstructor, this);

            if (this.Name.Name == NameFactory.ConvertFunctionName && type_owner != null)
            {
                if (this.Parameters.Any())
                    ctx.AddError(ErrorCode.ConverterWithParameters, this);
                if (!this.Modifier.HasPinned && !type_owner.Modifier.HasEnum && !type_owner.Modifier.IsSealed)
                    ctx.AddError(ErrorCode.ConverterNotPinned, this);
                if (this.CallMode != ExpressionReadMode.ReadRequired)
                    ctx.AddError(ErrorCode.ConverterDeclaredWithIgnoredOutput, this);
            }

            if (!this.IsAnyConstructor())
            {
                foreach (NameReference typename in this.Parameters.Select(it => it.TypeName))
                    typename.ValidateTypeNameVariance(ctx, VarianceMode.In);
                this.ResultTypeName.Cast<NameReference>().ValidateTypeNameVariance(ctx, VarianceMode.Out);
            }

            if (!ctx.Env.Options.AllowInvalidMainResult && this==ctx.Env.MainFunction && this.ResultTypeName.Evaluation.Components != ctx.Env.Nat8Type.InstanceOf)
                ctx.AddError(ErrorCode.MainFunctionInvalidResultType, this.ResultTypeName);

            if (this.Modifier.HasOverride && !this.Modifier.HasUnchainBase)
            {
                if (!this.IsDeclaration
                    && this.ContainingType().DerivationTable.TryGetSuper(this, out FunctionDefinition dummy)
                    && !this.DescendantNodes().WhereType<FunctionCall>().Any(it => it.Name.IsSuperReference))
                {
                    ctx.AddError(ErrorCode.DerivationWithoutSuperCall, this);
                }
            }

            if (!this.IsDeclaration && !this.Modifier.HasNative)
            {
                if (!ctx.Env.IsOfUnitType(this.ResultTypeName)
                    && !this.UserBody.Validation.IsTerminated)
                    ctx.AddError(ErrorCode.MissingReturn, this.UserBody);
            }

            FunctionParameter tail_anon_variadic = this.Parameters
                .Where(it => it.IsVariadic)
                .Skip(1) // first variadic can be anonymous
                .FirstOrDefault(it => !it.IsNameRequired);
            if (tail_anon_variadic != null)
                ctx.AddError(ErrorCode.AnonymousTailVariadicParameter, tail_anon_variadic);
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        public void SetIsMemberUsed()
        {
            this.IsMemberUsed = true;
        }

        public override void Surf(ComputationContext ctx)
        {
            if (this.DebugId.Id==1888)
            {
                ;
            }
            base.Surf(ctx);

            if (ctx.Env.IsUnitType(this.ResultTypeName.Evaluation.Components))
                this.CallMode = ExpressionReadMode.OptionalUse;
        }


        #region IExpression
        // function is an expression only for a moment, as lambda, until it is lifted as closure method
        // however we have to provide those membes to be compatible with interface
        int IExpression.DereferencedCount_LEGACY { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        int IExpression.DereferencingCount { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        ExpressionReadMode IExpression.ReadMode => throw new NotImplementedException();
        bool IExpression.IsRead { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        bool IExpression.IsLValue(ComputationContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}
