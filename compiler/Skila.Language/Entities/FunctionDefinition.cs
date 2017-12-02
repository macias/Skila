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
    public sealed class FunctionDefinition : TemplateDefinition, IEntity, IExecutableScope
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
                name, constraints, parameters, callMode, result, chainCall: null, body: body);
        }
        public static FunctionDefinition CreateFunction(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            FunctionCall chainCall,
            Block body)
        {
            return new FunctionDefinition(modifier,
                name, constraints, parameters, callMode, result, chainCall, body);
        }

        internal void SetModifier(EntityModifier modifier)
        {
            this.Modifier = modifier;
        }

        public static FunctionDefinition CreateInitConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            Block body)
        {
            return new FunctionDefinition(modifier,
                                NameFactory.InitConstructorNameDefinition(), null,
                                parameters, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
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
                chainCall: null, body: body);
        }
        public static FunctionDefinition CreateZeroConstructor(Block body)
        {
            return new FunctionDefinition(EntityModifier.None,
                                NameFactory.ZeroConstructorNameDefinition(), null,
                                null, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
        }

        public bool IsResultTypeNameInfered { get; }
        public INameReference ResultTypeName { get; private set; }
        private readonly List<IEntityInstance> resultTypeCandidates;
        public Block UserBody { get; }
        public IReadOnlyList<FunctionParameter> Parameters { get; }
        public FunctionParameter MetaThisParameter { get; private set; }
        private NameReference thisNameReference;
        public ExpressionReadMode CallMode { get; }
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(UserBody);

        internal LambdaTrap LambdaTrap { get; set; }

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            // parameters have to go before user body, so they are registered for use
            .Concat(this.Parameters)
            .Concat(this.MetaThisParameter)
            .Concat(UserBody)
            .Concat(this.ResultTypeName)
            .Concat(this.thisNameReference)
            .Where(it => it != null);

        public override IEnumerable<ISurfable> Surfables => base.Surfables.Concat(this.Parameters).Concat(ResultTypeName);

        // we keep this as a shortcut for particular piece of the body (initially chain call is not part of the body)
        private readonly FunctionCall constructorChainCall;
        private FunctionCall constructorZeroCall;

        public bool IsDeclaration => this.UserBody == null;

        public bool IsLambdaInvoker => this.Name.Name == NameFactory.LambdaInvoke;
        public bool IsLambda => this.EnclosingScope<TemplateDefinition>().IsFunction();

        private FunctionDefinition(EntityModifier modifier,
            NameDefinition name,
            IEnumerable<TemplateConstraint> constraints,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            FunctionCall chainCall,
            Block body)
            : base(modifier | (body == null ? EntityModifier.Abstract : EntityModifier.None), name, constraints)
        {
            parameters = parameters ?? Enumerable.Empty<FunctionParameter>();

            this.constructorChainCall = chainCall;
            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.ResultTypeName = result;
            this.IsResultTypeNameInfered = result == null;
            if (this.IsResultTypeNameInfered)
                this.resultTypeCandidates = new List<IEntityInstance>();
            this.UserBody = body;
            this.CallMode = callMode;

            if (this.constructorChainCall != null)
                this.UserBody.Prepend(this.constructorChainCall);

            if (this.IsLambdaInvoker)
                this.LambdaTrap = new LambdaTrap();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.constructionCompleted = true;
        }

        public NameReference CreateFunctionInterface()
        {
            return NameFactory.FunctionTypeReference(this.Parameters.Select(it => it.TypeName), this.ResultTypeName);
        }

        internal void AddResultTypeCandidate(INameReference typenameCandidate)
        {
            this.resultTypeCandidates.Add(typenameCandidate.Evaluation.Components);
        }

        internal void InferResultType(ComputationContext ctx)
        {
            if (!this.resultTypeCandidates.Any()) // no returns
                this.ResultTypeName = ctx.Env.VoidType.InstanceOf.NameOf;
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
                    // it is tempting to allowing conversions here, but it would mean that we have back to all "returns"
                    // to apply such conversions, besides such fluent result type is a bit of a stretch
                    if (candidate.MatchesTarget(ctx, common, allowSlicing: false) != TypeMatch.Pass)
                    {
                        ctx.AddError(ErrorCode.CannotInferResultType, this);
                        this.ResultTypeName = EntityInstance.Joker.NameOf;
                        return;
                    }

                this.ResultTypeName = common.NameOf;
            }
        }


        public override bool AttachTo(INode parent)
        {
            if (this.DebugId.Id == 7172)
            {
                ;
            }

            bool result = base.AttachTo(parent);

            TypeDefinition owner_type = this.OwnerType();
            if (this.MetaThisParameter == null && owner_type != null) // method
            {
                NameReference type_name = owner_type.InstanceOf.NameOf; // initially already tied to target
                this.MetaThisParameter = FunctionParameter.Create(NameFactory.ThisVariableName,
                    NameFactory.ReferenceTypeReference(type_name),
                    Variadic.None, null, isNameRequired: false);
                this.MetaThisParameter.AttachTo(this);
            }

            if (owner_type != null && this.IsInitConstructor() && owner_type.ZeroConstructor != null)
            {
                this.constructorZeroCall = FunctionCall.Create(NameReference.Create(NameFactory.ZeroConstructorName));
                this.UserBody.Prepend(constructorZeroCall);
            }

            if (result && parent is TypeContainerDefinition && !this.Modifier.HasAccessSet)
                this.SetModifier(this.Modifier | EntityModifier.Public);

            return result;
        }

        public override string ToString()
        {
            return base.ToString() + "(" + this.Parameters.Select(it => it.ToString()).Join(",") + $") -> {this.ResultTypeName}";
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
            this.IsComputed = true;
        }

        public override void Validate(ComputationContext ctx)
        {
            if (this.Modifier.HasRefines && !this.Modifier.HasUnchainBase)
            {
                if (!this.IsDeclaration
                    && this.OwnerType().DerivationTable.TryGetSuper(this,out FunctionDefinition dummy)
                    && !this.DescendantNodes().WhereType<FunctionCall>().Any(it => it.Name.IsSuperReference))
                {
                    ctx.AddError(ErrorCode.DerivationWithoutSuperCall, this);
                }
            }

            if (!this.IsDeclaration)
            {
                if (!ctx.Env.IsOfVoidType(this.ResultTypeName)
                    && !ctx.Env.IsOfUnitType(this.ResultTypeName)
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

        #region IExpression
        // function is an expression only for a moment, as lambda, until it is lifted as closure method
        // however we have to provide those membes to be compatible with interface
        bool IExpression.IsDereferenced { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        bool IExpression.IsDereferencing { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        ExpressionReadMode IExpression.ReadMode => throw new NotImplementedException();
        bool IExpression.IsRead { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        bool IExpression.IsLValue(ComputationContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}
