﻿using System.Collections.Generic;
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

        public NameReference TypeName { get; }
        public INameReference ResultTypeName => this.TypeName.TemplateArguments.Last();
        public Block UserBody { get; }
        public IReadOnlyList<FunctionParameter> Parameters { get; }
        public FunctionParameter MetaThisParameter { get; private set; }
        private NameReference thisNameReference;
        public ExpressionReadMode CallMode { get; }
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(UserBody);

        internal LambdaTrap LambdaTrap { get; set; }

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(TypeName, UserBody)
            .Concat(this.Parameters)
            .Concat(this.MetaThisParameter)
            .Concat(this.thisNameReference)
            .Where(it => it != null);

        // we keep this as a shortcut for particular piece of the body (initially chain call is not part of the body)
        private readonly FunctionCall constructorChainCall;
        private FunctionCall constructorZeroCall;

        public bool IsDeclaration => this.UserBody == null;
        public bool IsAbstract => this.IsDeclaration || this.Modifier.HasAbstract;
        // sealed functions cannot be derived from
        public bool IsSealed => !this.IsAbstract && !this.Modifier.HasBase;
        public bool IsVirtual => this.IsAbstract || this.Modifier.HasDerived || this.Modifier.HasBase;

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
            : base(modifier, name, constraints)
        {
            parameters = parameters ?? Enumerable.Empty<FunctionParameter>();

            this.constructorChainCall = chainCall;
            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.TypeName = NameFactory.FunctionTypeReference(parameters.Select(it => it.TypeName), result);
            this.UserBody = body;
            this.CallMode = callMode;

            if (this.constructorChainCall != null)
                this.UserBody.Prepend(this.constructorChainCall);

            if (this.IsLambdaInvoker)
                this.LambdaTrap = new LambdaTrap();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.constructionCompleted = true;
        }

        public override void AttachTo(INode parent)
        {
            if (this.DebugId.Id == 7172)
            {
                ;
            }
            base.AttachTo(parent);

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
        }

        public override string ToString()
        {
            return base.ToString() + "(" + this.Parameters.Select(it => it.ToString()).Join(",") + ")";
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
            if (!this.IsComputed)
            {
                this.Evaluation = TypeName.Evaluated(ctx);
                this.IsComputed = true;

                {
                    FunctionParameter tail_anon_variadic = this.Parameters
                        .Where(it => it.IsVariadic)
                        .Skip(1) // first variadic can be anonymous
                        .FirstOrDefault(it => !it.IsNameRequired);
                    if (tail_anon_variadic != null)
                        ctx.AddError(ErrorCode.AnonymousTailVariadicParameter, tail_anon_variadic);
                }

            }
        }

        public override void Validate(ComputationContext ctx)
        {
            if (!this.IsDeclaration)
            {
                if (!ctx.Env.IsOfVoidType(this.ResultTypeName)
                    && !ctx.Env.IsOfUnitType(this.ResultTypeName)
                    && !this.UserBody.Validation.IsTerminated)
                    ctx.AddError(ErrorCode.MissingReturn, this.UserBody);
            }
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }

        #region IExpression
        // function is an expression only for a moment, as lambda, until it is lifted as closure method
        // however we have to provide those membes to be compatible with interface
        bool IExpression.IsDereferenced { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        ExpressionReadMode IExpression.ReadMode => throw new NotImplementedException();
        bool IExpression.IsRead { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        bool IExpression.IsLValue(ComputationContext ctx)
        {
            throw new NotImplementedException();
        }
        #endregion
    }

}
