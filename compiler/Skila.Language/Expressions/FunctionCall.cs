using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using Skila.Language.Semantics;
using Skila.Language.Entities;

namespace Skila.Language.Expressions
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class FunctionCall : Node, IExpression, IFunctionArgumentsProvider, ILambdaTransfer
    {
        public static FunctionCall Create(IExpression name, params FunctionArgument[] arguments)
        {
            return new FunctionCall(false, name, arguments, requestedOutcomeType: null);
        }
        public static FunctionCall Constructor(IExpression name, params FunctionArgument[] arguments)
        {
            return new FunctionCall(true, name, arguments, requestedOutcomeType: null);
        }
        public static FunctionCall Constructor(string name, params FunctionArgument[] arguments)
        {
            return Constructor(NameReference.Create(name), arguments);
        }
        public static FunctionCall CreateToCall(IExpression expr, NameReference typeName)
        {
            return new FunctionCall(false, NameReference.Create(expr, NameFactory.ConvertFunctionName),
                arguments: null, requestedOutcomeType: typeName);
        }

        private bool? isRead;
        public bool IsRead
        {
            get { return this.isRead.Value; }
            set
            {
                if (this.DebugId.Id == 3131)
                {
                    ;
                }
                if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value;
            }
        }

        private readonly List<TypeDefinition> closures;
        private Option<CallResolution> resolution;
        public CallResolution Resolution => this.resolution.Value;
        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsDereferenced { get; set; }
        public bool IsDereferencing { get; set; }
        // public bool IsStaticCall => this.Resolution.TargetInstance.Target.Modifier.HasStatic;

        // eventually some vague callee expression will become a name reference to a function
        private IExpression callee;
        public IExpression Callee => this.callee;
        public NameReference Name => this.Callee.Cast<NameReference>();

        public IReadOnlyList<FunctionArgument> Arguments { get; }
        public override IEnumerable<INode> OwnedNodes => Arguments.Select(it => it.Cast<INode>())
            .Concat(this.Callee)
            .Concat(RequestedOutcomeTypeName)
            .Where(it => it != null)
            .Concat(closures);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Arguments);

        public INameReference RequestedOutcomeTypeName { get; }

        public ExpressionReadMode ReadMode => this.Resolution?.TargetFunction?.CallMode ?? ExpressionReadMode.OptionalUse;
        private readonly bool isConstructorCall;

        private FunctionCall(bool isConstructorCall,
            IExpression callee, IEnumerable<FunctionArgument> arguments, NameReference requestedOutcomeType)
          : base()
        {
            this.isConstructorCall = isConstructorCall;
            this.callee = callee;
            this.Arguments = (arguments ?? Enumerable.Empty<FunctionArgument>()).Indexed().StoreReadOnlyList();
            this.RequestedOutcomeTypeName = requestedOutcomeType;

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return this.Callee + "(" + Arguments.Select(it => it.ToString()).Join(",") + ")";
        }

        public void Validate(ComputationContext ctx)
        {
            if (this.Resolution == null)
                return;

            FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();

            if (this.Resolution.TargetFunction.Modifier.IsVirtual
                && enclosing_func != null && enclosing_func.IsConstructor()
                && !enclosing_func.OwnerType().Modifier.IsSealed)
                ctx.AddError(ErrorCode.VirtualCallFromConstructor, this);

            if (!this.isConstructorCall && this.Resolution.TargetFunction.IsConstructor())
                ctx.AddError(ErrorCode.ConstructorCallFromFunctionBody, this);

            if (this.Name.Binding.Match.Target is FunctionDefinition binding_func)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if (this.Name.Name != NameFactory.SelfFunctionName && binding_func == func)
                    ctx.ErrorManager.AddError(ErrorCode.NamedRecursiveReference, this.Name);
                else if (!this.Name.IsSuperReference && func != null)
                {
                    func = func.TryGetSuperFunction(ctx);
                    if (func == binding_func)
                        ctx.ErrorManager.AddError(ErrorCode.NamedRecursiveReference, this.Name);
                }
            }
            

        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 155)
                {
                    ;
                }

                // trap only lambdas, name reference is a call, not passing function around
                // for example here trapping lambda into closure is necessary
                // ((x) => x*x)()
                // and here is not (regular call)
                // f()
                if (this.TrapLambdaClosure(ctx, ref this.callee))
                    ConvertToExplicitInvoke(ctx);

                {
                    EntityInstance eval = this.Callee.Evaluation.Components.Cast<EntityInstance>();

                    this.Callee.IsDereferenced = ctx.Env.Dereferenced(eval, out IEntityInstance __eval, out bool via_pointer);
                    this.IsDereferencing = this.Callee.IsDereferenced;
                    if (this.Callee.IsDereferenced)
                        eval = __eval.Cast<EntityInstance>();

                    if (!(this.Name.Binding.Match.Target is FunctionDefinition)
                         && eval.Target.Cast<TypeDefinition>().InvokeFunctions().Any())
                    {
                        // if we call a "closure", like my_closure() it is implicit calling "invoke"
                        // so make it explicit on the fly
                        ConvertToExplicitInvoke(ctx);
                    }
                }

                IEnumerable<EntityInstance> matches = this.Name.Binding.Matches
                    .Where(it => it.Target.IsFunction());

                if (!matches.Any())
                {
                    this.resolution = new Option<CallResolution>(null);
                    if (!this.Callee.Evaluation.Components.IsJoker) // do not cascade errors
                        ctx.AddError(ErrorCode.NotFunctionType, this.Callee);
                }
                else
                {
                    IEnumerable<CallResolution> targets = matches
                        .Select(it => CallResolution.Create(ctx, this.Name.TemplateArguments, this,
                            createCallContext(ctx, this.Name, it.Target), targetInstance: it))
                        .Where(it => it != null)
                        .StoreReadOnly();
                    targets = targets.Where(it => it.AllArgumentsMapped()).StoreReadOnly();
                    targets = targets.Where(it => it.RequiredParametersUsed()).StoreReadOnly();
                    targets = targets.Where(it => it.ArgumentTypesMatchParameters(ctx)).StoreReadOnly();
                    if (this.RequestedOutcomeTypeName != null)
                        targets = targets.Where(it => it.OutcomeMatchesRequest(ctx)).StoreReadOnly();

                    if (this.DebugId.Id == 2023)
                    {
                        ;
                    }

                    targets = resolveOverloading(targets).StoreReadOnly();

                    this.resolution = new Option<CallResolution>(targets.FirstOrDefault());

                    if (!targets.Any())
                        ctx.AddError(ErrorCode.TargetFunctionNotFound, this);
                    else
                    {
                        if (targets.Count() > 1)
                        {
                            ctx.ErrorManager.AddError(ErrorCode.NOTEST_AmbiguousOverloadedCall, this,
                                targets.Select(it => it.TargetFunctionInstance.Target));
                        }

                        this.Resolution.SetMappings();

                        if (targets.Count() == 1)
                        {
                            this.Resolution.EnhanceArguments(ctx);
                            /*  if (this.Resolution.TargetInstance.Target.CastFunction().IsInitConstructor() 
                                  && this.Resolution.ObjectInstanceType) */
                        }

                        // filtering here is a bit shaky -- if we don't use type inference
                        // we have to filter by what we bind to, but if we use inference
                        // we use target instance (functor or function, not a variable) because only it
                        // is altered by type inference

                        if (this.Resolution.InferredTemplateArguments == null)
                        {
                            // leave only binding which was used for mapping
                            this.Name.Binding.Filter(it => it == this.Resolution.TargetFunctionInstance);
                        }
                        else
                        {
                            this.callee = this.Name.Recreate(this.Resolution.InferredTemplateArguments);

                            this.Callee.Evaluated(ctx);

                            this.Name.Binding.Filter(it => it == this.Resolution.TargetFunctionInstance);
                        }

                        if (this.DebugId.Id == 228)
                        {
                            ;
                        }

                        this.Evaluation = this.Resolution.Evaluation;
                    }
                }


                if (this.Evaluation == null)
                {
                    this.Evaluation = EvaluationInfo.Joker;
                }

                foreach (IExpression arg in Arguments)
                    arg.ValidateValueExpression(ctx);

                if (this.Resolution != null)
                {
                    foreach (var group in this.Resolution.GetArgumentsMultipleTargeted())
                        // we only report second "override" because if there are more 
                        // it is more likely user forgot to mark parameter variadic
                        ctx.ErrorManager.AddError(ErrorCode.ArgumentForFunctionAlreadyGiven, group.Skip(1).FirstOrDefault());

                    foreach (FunctionParameter param in this.Resolution.GetUnfulfilledVariadicParameters())
                        ctx.ErrorManager.AddError(ErrorCode.InvalidNumberVariadicArguments, this, param);
                }
            }
        }

        private void ConvertToExplicitInvoke(ComputationContext ctx)
        {
            this.Callee.DetachFrom(this);
            this.callee = NameReference.Create(this.Callee, NameFactory.LambdaInvoke);
            this.callee.AttachTo(this);
            this.callee.Evaluated(ctx);
        }

        private static CallContext createCallContext(ComputationContext ctx, NameReference name, IEntity callTarget)
        {
            IExpression this_context = name.GetContext(callTarget);
            if (this_context == null)
                return new CallContext();

            this_context.Evaluated(ctx);

            if (callTarget.Modifier.HasStatic)
                return new CallContext() { StaticContext = this_context.Evaluation.Components };
            else
                return new CallContext() { MetaThisArgument = FunctionArgument.Create(this_context) };
        }

        private static IEnumerable<CallResolution> resolveOverloading(IEnumerable<CallResolution> targets)
        {
            if (targets.Count() < 2)
                return targets;

            // the less, the better
            var arguments_matches = new List<Tuple<CallResolution, List<int>>>();
            foreach (CallResolution call_target in targets)
            {
                List<int> matches = call_target.Arguments
                    .Select(arg =>
                    {
                        FunctionParameter param = call_target.GetParamByArgIndex(arg.Index);
                        int weight = 0;
                        // prefer non-variadic parameters
                        if (param.IsVariadic)
                            weight += 1;
                        // prefer concrete type over generic one (foo(Int) better than foo<T>(T))
                        // note we use untranslated param evaluation here to achieve this effect
                        if (!param.Evaluation.Components.IsSame(arg.Evaluation.Components, jokerMatchesAll: true))
                            weight += 2;

                        // prefer exact match instead more general match (Int->Int is better than Int->Object)
                        IEntityInstance param_trans_eval = call_target.GetTransParamEvalByArgIndex(arg.Index);
                        TypeMatch m = call_target.TypeMatches[arg.Index].Value;
                        if (m.HasFlag(TypeMatch.Substitute))
                            weight += 4;
                        else if (!m.HasFlag(TypeMatch.Same)) // conversions
                            weight += 8;

                        return weight;
                    })
                    .ToList();
                // bonus if optional parameters are explicitly targeted (i.e. default values are not used)
                matches.Add(call_target.AllParametersUsed() ? 0 : 1);

                arguments_matches.Add(Tuple.Create(call_target, matches));
            }

            Option<Tuple<CallResolution, List<int>>> best = arguments_matches
                .IntransitiveMin((a, b) => Extensions.Tools.HasOneLessNoneGreaterThan(a.Item2, b.Item2, (x, y) => x < y));

            if (best.HasValue)
                return new[] { best.Value.Item1 };
            else
                return targets;
        }

        public bool IsReadingValueOfNode(IExpression node)
        {
            return true;
        }
        public bool IsLValue(ComputationContext ctx)
        {
            return !this.Evaluation.Components.IsValueType(ctx);
        }
        public void AddClosure(TypeDefinition closure)
        {
            this.closures.Add(closure);
            closure.AttachTo(this);
        }
    }
}
