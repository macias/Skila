using System.Collections.Generic;
using System.Diagnostics;
using NaiveLanguageTools.Common;
using Skila.Language.Expressions;
using System.Linq;
using Skila.Language.Extensions;
using Skila.Language.Comparers;
using System;
using Skila.Language.Semantics;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionDefinition : TemplateDefinition, IExpression, INode, IEntity,
        IExecutableScope, IFunctionSignature, IFunctionOutcome
    {
        public static FunctionDefinition CreateFunction(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            Block body)
        {
            return new FunctionDefinition(ExpressionReadMode.CannotBeRead, modifier,
                name, parameters, callMode, result, chainCall: null, body: body);
        }
        public static FunctionDefinition CreateDeclaration(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result)
        {
            return new FunctionDefinition(ExpressionReadMode.CannotBeRead, modifier,
                name, parameters, callMode, result, chainCall: null, body: null);
        }
        public static FunctionDefinition CreateFunction(
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode callMode,
            INameReference result,
            FunctionCall chainCall,
            Block body)
        {
            return new FunctionDefinition( ExpressionReadMode.CannotBeRead, modifier,
                name, parameters, callMode, result, chainCall, body);
        }
        public static FunctionDefinition CreateInitConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            Block body)
        {
            return new FunctionDefinition(ExpressionReadMode.CannotBeRead, modifier,
                                NameFactory.InitConstructorNameDefinition(),
                                parameters, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
        }
        public static FunctionDefinition CreateHeapConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            NameReference typeName,
            Block body)
        {
            return new FunctionDefinition( ExpressionReadMode.CannotBeRead,
                modifier | EntityModifier.Static,
                NameFactory.NewConstructorNameDefinition(),
                parameters, ExpressionReadMode.ReadRequired, NameFactory.PointerTypeReference(typeName),
                chainCall: null, body: body);
        }
        public static FunctionDefinition CreateZeroConstructor(Block body)
        {
            return new FunctionDefinition( ExpressionReadMode.CannotBeRead, EntityModifier.None,
                                NameFactory.ZeroConstructorNameDefinition(),
                                null, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }
        public bool IsDereferenced { get; set; }

        public NameReference FunctorTypeName { get; }
        public INameReference ResultTypeName => this.FunctorTypeName.TemplateArguments.Last();
        public Block UserBody { get; }
        public IReadOnlyList<FunctionParameter> Parameters { get; }
        public FunctionParameter MetaThisParameter { get; private set; }
        private NameReference thisNameReference;
        public ExpressionReadMode CallMode { get; }
        public ExpressionReadMode ReadMode { get; }
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(UserBody);

        public override IEnumerable<INode> OwnedNodes => base.OwnedNodes
            .Concat(FunctorTypeName, UserBody)
            .Concat(this.Parameters)
            .Concat(this.MetaThisParameter)
            .Concat(this.thisNameReference)
            .Where(it => it != null);

        // we keep this as a shortcut for particular piece of the body (initially chain call is not part of the body)
        private readonly FunctionCall constructorChainCall;
        private readonly FunctionCall constructorZeroCall;

        public bool IsDeclaration => this.UserBody == null;
        public bool IsAbstract => this.IsDeclaration || this.Modifier.HasAbstract;
        // sealed functions cannot be derived from
        public bool IsSealed => !this.IsAbstract && !this.Modifier.HasBase;
        public bool IsVirtual => this.IsAbstract || this.Modifier.HasDerived || this.Modifier.HasBase;

        private FunctionDefinition(ExpressionReadMode readMode,
            EntityModifier modifier,
            NameDefinition name,
            IEnumerable<FunctionParameter> parameters,
            ExpressionReadMode resultMode,
            INameReference result,
            FunctionCall chainCall,
            Block body)
            : base(modifier, name)
        {
            parameters = parameters ?? Enumerable.Empty<FunctionParameter>();

            this.constructorChainCall = chainCall;
            this.ReadMode = readMode;
            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.FunctorTypeName = NameFactory.CreateFunction(parameters.Select(it => it.TypeName), result);
            this.UserBody = body;
            this.CallMode = resultMode;

            if (this.IsInitConstructor())
            {
                this.constructorZeroCall = FunctionCall.Create(NameReference.Create(NameFactory.ZeroConstructorName));
                this.UserBody.Prepend(constructorZeroCall);
            }

            if (this.constructorChainCall != null)
                this.UserBody.Prepend(this.constructorChainCall);

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
                NameReference type_name = owner_type.Name.CreateNameReference();
                this.MetaThisParameter = FunctionParameter.Create(NameFactory.ThisVariableName,
                    NameFactory.ReferenceTypeReference(type_name),
                    Variadic.None, null, isNameRequired: false);
                this.MetaThisParameter.AttachTo(this);
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
                this.Evaluation = FunctorTypeName.Evaluated(ctx);
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
        public bool IsLValue(ComputationContext ctx)
        {
            return false;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return false;
        }
    }

}
