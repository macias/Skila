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
            return new FunctionDefinition(FunctionRole.Other, ExpressionReadMode.CannotBeRead, modifier,
                name, parameters, callMode, result, chainCall: null, body: body);
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
            return new FunctionDefinition(FunctionRole.Other, ExpressionReadMode.CannotBeRead, modifier,
                name, parameters, callMode, result, chainCall, body);
        }
        public static FunctionDefinition CreateInitConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            Block body)
        {
            return new FunctionDefinition(FunctionRole.InitConstructor, ExpressionReadMode.CannotBeRead, modifier,
                                NameFactory.InitConstructorNameDefinition(),
                                parameters, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
        }
        public static FunctionDefinition CreateHeapConstructor(
            EntityModifier modifier,
            IEnumerable<FunctionParameter> parameters,
            NameReference typeName,
            Block body)
        {
            return new FunctionDefinition(FunctionRole.NewConstructor, ExpressionReadMode.CannotBeRead,
                modifier | EntityModifier.Static,
                NameFactory.NewConstructorNameDefinition(),
                parameters, ExpressionReadMode.ReadRequired, NameFactory.PointerTypeReference(typeName),
                chainCall: null, body: body);
        }
        public static FunctionDefinition CreateZeroConstructor(Block body)
        {
            return new FunctionDefinition(FunctionRole.ZeroConstructor, ExpressionReadMode.CannotBeRead, EntityModifier.None,
                                NameFactory.ZeroConstructorNameDefinition(),
                                null, ExpressionReadMode.CannotBeRead, NameFactory.VoidTypeReference(), chainCall: null, body: body);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }
        public bool IsDereferenced { get; set; }

        public FunctionRole Role { get; }
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

        private FunctionDefinition(FunctionRole role,
            ExpressionReadMode readMode,
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
            this.Role = role;
            this.ReadMode = readMode;
            this.Parameters = parameters.Indexed().StoreReadOnlyList();
            this.FunctorTypeName = NameFactory.CreateFunction(parameters.Select(it => it.TypeName), result);
            this.UserBody = body ?? Block.CreateStatement(null);
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

        internal static bool IsOverloadedDuplicate(FunctionDefinition f1, FunctionDefinition f2)
        {
            // since in case of functions type parameters can be inferred it is better to exclude arity
            // when checking if two functions are duplicates -- let the function parameter types decide
            if (!EntityBareNameComparer.Instance.Equals(f1.Name, f2.Name))
                return false;

            {   // linear check of anonymous parameters types

                // we move optional parameters at the end, because they have to be at the end (among anonymous ones)
                IEnumerable<FunctionParameter> f1_params = f1.Parameters.Where(it => !it.IsNameRequired)
                    .OrderBy(it => it.IsOptional)
                    .Concat((FunctionParameter)null); // terminal for easier checking
                IEnumerable<FunctionParameter> f2_params = f2.Parameters.Where(it => !it.IsNameRequired)
                    .OrderBy(it => it.IsOptional)
                    .Concat((FunctionParameter)null);

                foreach (Tuple<FunctionParameter, FunctionParameter> param_pair in f1_params.Zip(f2_params, (a, b) => Tuple.Create(a, b)))
                {
                    if (param_pair.Item1 == null)
                    {
                        if (param_pair.Item2 == null || param_pair.Item2.IsOptional)
                            break;
                        // when we hit terminal and in the other function we still have non-optional we have a difference
                        else
                            return false;
                    }
                    if (param_pair.Item2 == null)
                    {
                        if (param_pair.Item1 == null || param_pair.Item1.IsOptional)
                            break;
                        else
                            return false;
                    }

                    if (param_pair.Item1.IsOptional && param_pair.Item2.IsOptional)
                        break;

                    if (param_pair.Item1.TypeName.Evaluation.IsOverloadDistinctFrom(param_pair.Item2.TypeName.Evaluation)
                        || param_pair.Item1.IsVariadic != param_pair.Item2.IsVariadic)
                        return false;
                }
            }

            {
                // checking non-optional parameters with required names
                Dictionary<ITemplateName, FunctionParameter> f1_params = f1.Parameters.Where(it => it.IsNameRequired && !it.IsOptional)
                    .ToDictionary(it => it.Name, it => it, EntityBareNameComparer.Instance);
                IEnumerable<FunctionParameter> f2_params = f2.Parameters.Where(it => it.IsNameRequired && !it.IsOptional).StoreReadOnly();

                if (f1_params.Count() != f2_params.Count())
                    return false;

                foreach (FunctionParameter f2_param in f2_params)
                {
                    FunctionParameter f1_param;
                    if (!f1_params.TryGetValue(f2_param.Name, out f1_param))
                        return false;
                    if (f1_param.TypeName.Evaluation.IsOverloadDistinctFrom(f2_param.TypeName.Evaluation))
                        return false;
                }
            }

            if (f1.IsOutConverter() && f2.IsOutConverter())
                if (f1.ResultTypeName.Evaluation.IsOverloadDistinctFrom(f2.ResultTypeName.Evaluation))
                    return false;

            return true;
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
