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
        private enum CallMode
        {
            Constructor,
            Indexer,
            Regular
        }
        public static FunctionCall Indexer(IExpression expr, params IExpression[] arguments)
        {
            return Indexer(expr, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static FunctionCall Indexer(IExpression expr, params FunctionArgument[] arguments)
        {
            // we create indexer-getters for all cases initially, then we have to only check assignments LHS
            // and convert only those to indexer-setters
            return new FunctionCall(CallMode.Indexer,
                // alternative approach (below, commented out) would be to refer indexer accessor indirectly, 
                // via indexer itself, so instead of:
                // my_obj.|INDEXER-MODE|Get(5)
                // we would have:
                // my_obj.idx.idxGet(5)
                // the latter approach seems cleaner but requires more handling in code
                NameReference.Create(expr, NameFactory.PropertyGetter),
                // NameReference.Create(NameReference.Create(name, NameFactory.PropertyIndexerName), NameFactory.PropertyGetter),
                arguments,
                requestedOutcomeType: null);
        }
        public static FunctionCall Create(IExpression name)
        {
            return Create(name, Enumerable.Empty<FunctionArgument>().ToArray());
        }
        public static FunctionCall Create(IExpression name, params IExpression[] arguments)
        {
            return Create(name, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static FunctionCall Create(IExpression name, params FunctionArgument[] arguments)
        {
            return new FunctionCall(CallMode.Regular, name, arguments, requestedOutcomeType: null);
        }
        public static FunctionCall Constructor(IExpression name)
        {
            return Constructor(name, new FunctionArgument[] { });
        }
        public static FunctionCall Constructor(IExpression name, params FunctionArgument[] arguments)
        {
            return new FunctionCall(CallMode.Constructor, name, arguments, requestedOutcomeType: null);
        }
        public static FunctionCall Constructor(IExpression name, params IExpression[] arguments)
        {
            return Constructor(name, arguments.Select(it => FunctionArgument.Create(it)).ToArray());
        }
        public static FunctionCall Constructor(string name, params FunctionArgument[] arguments)
        {
            return Constructor(NameReference.Create(name), arguments);
        }
        public static FunctionCall ConvCall(IExpression expr, NameReference typeName)
        {
            return new FunctionCall(CallMode.Regular, NameReference.Create(expr, NameFactory.ConvertFunctionName),
                arguments: null, requestedOutcomeType: typeName);
        }

        private bool? isRead;
        public bool IsRead
        {
            get { return this.isRead.Value; }
            set
            {
                if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value;
            }
        }

        private readonly List<TypeDefinition> closures;
        private Option<CallResolution> resolution;
        public CallResolution Resolution => this.resolution.Value;
        public bool IsComputed => this.Evaluation != null;
        public EvaluationInfo Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        private int dereferencedCount { get; set; }
        public int DereferencedCount_LEGACY
        {
            get { return this.dereferencedCount; }
            set
            {
                this.dereferencedCount = value;
            }
        }
        public int DereferencingCount { get; set; }
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
        private readonly Later<ExecutionFlow> flow;
        public ExecutionFlow Flow => this.flow.Value;

        public INameReference RequestedOutcomeTypeName { get; }

        public ExpressionReadMode ReadMode => this.Resolution?.TargetFunction?.CallMode ?? ExpressionReadMode.OptionalUse;
        private readonly CallMode mode;

        public bool IsIndexer => this.mode == CallMode.Indexer;

        private FunctionCall(CallMode mode,
            IExpression callee, IEnumerable<FunctionArgument> arguments, NameReference requestedOutcomeType)
          : base()
        {
            this.mode = mode;
            this.callee = callee;
            this.Arguments = (arguments ?? Enumerable.Empty<FunctionArgument>()).Indexed().StoreReadOnlyList();
            this.RequestedOutcomeTypeName = requestedOutcomeType;

            this.closures = new List<TypeDefinition>();

            this.OwnedNodes.ForEach(it => it.AttachTo(this));

            this.flow = new Later<ExecutionFlow>(() => ExecutionFlow.CreatePath(Arguments));
        }
        public override string ToString()
        {
            return this.Callee + "(" + Arguments.Select(it => it == null ? "Ø" : it.ToString()).Join(",") + ")";
        }

        public void Validate(ComputationContext ctx)
        {
            if (this.Resolution == null)
                return;

            FunctionDefinition enclosing_func = this.EnclosingScope<FunctionDefinition>();

            if (this.Resolution.TargetFunction.Modifier.IsPolymorphic
                && enclosing_func != null && enclosing_func.IsAnyConstructor()
                && !enclosing_func.ContainingType().Modifier.IsSealed)
                ctx.AddError(ErrorCode.VirtualCallFromConstructor, this);

            if (this.mode != CallMode.Constructor && this.Resolution.TargetFunction.IsAnyConstructor())
                ctx.AddError(ErrorCode.ConstructorCallFromFunctionBody, this);

            {
                bool is_recall = isRecall(out FunctionDefinition curr_func, out FunctionDefinition binding_func);
                if (!ctx.Env.Options.AllowNamedSelf && binding_func != null)
                {
                    if (this.Name.Name != NameFactory.SelfFunctionName && is_recall)
                        ctx.ErrorManager.AddError(ErrorCode.NamedRecursiveFunctionReference, this.Name);
                    else if (!this.Name.IsSuperReference && curr_func != null)
                    {
                        FunctionDefinition super_func = curr_func.TryGetSuperFunction(ctx);
                        if (super_func == binding_func)
                            ctx.ErrorManager.AddError(ErrorCode.NamedRecursiveFunctionReference, this.Name);
                    }
                }
            }

            {
                if (this.Name.TargetsCurrentInstanceMember(out IMember member))
                {
                    FunctionDefinition callee = member.CastFunction();
                    FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                    if (!func.Modifier.HasMutable && !func.IsAnyConstructor() && callee.Modifier.HasMutable)
                    {
                        ctx.AddError(ErrorCode.CallingMutableFromImmutableMethod, this);
                    }
                }
            }

            if (this.Resolution.MetaThisArgument != null)
            {
                // we cannot call mutable methods on neutral instance as well, because in such case we could
                // pass const instance (of mutable type) as neutral instance (aliasing const instance)
                // and then call mutable method making "const" guarantee invalid

                TypeMutability this_mutability = this.Resolution.MetaThisArgument.Expression.Evaluation.Components.MutabilityOfType(ctx);
                if (this_mutability != TypeMutability.Mutable && this.Resolution.TargetFunction.Modifier.HasMutable)
                    ctx.AddError(ErrorCode.AlteringNonMutableInstance, this);
            }


            if (this.Resolution.TargetFunction.Modifier.HasHeapOnly)
            {
                FunctionDefinition func = this.EnclosingScope<FunctionDefinition>();
                if ((this.Name.Prefix != null && !ctx.Env.IsPointerOfType(this.Name.Prefix.Evaluation.Components))
                    || (this.Name.Prefix == null && !func.Modifier.HasHeapOnly))
                    ctx.AddError(ErrorCode.CallingHeapFunctionWithValue, this);
            }
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                // trap only lambdas, name reference is a call, not passing function around
                // for example here trapping lambda into closure is necessary
                // ((x) => x*x)()
                // and here is not (regular call)
                // f()
                if (this.TrapLambdaClosure(ctx, ref this.callee))
                    ConvertToExplicitInvoke(ctx);

                {
                    EntityInstance eval = this.Callee.Evaluation.Components.Cast<EntityInstance>();

                    this.Callee.DereferencedCount_LEGACY = ctx.Env.DereferencedOnce(eval, out IEntityInstance __eval, out bool via_pointer) ? 1 : 0;
                    this.DereferencingCount = this.Callee.DereferencedCount_LEGACY;
                    if (this.Callee.DereferencedCount_LEGACY > 0)
                        eval = __eval.Cast<EntityInstance>();

                    if (!(this.Name.Binding.Match.Instance.Target is FunctionDefinition)
                         && eval.Target.Cast<TypeDefinition>().InvokeFunctions().Any())
                    {
                        // if we call a "closure", like my_closure() it is implicit calling "invoke"
                        // so make it explicit on the fly
                        ConvertToExplicitInvoke(ctx);
                    }
                }

                IEnumerable<EntityInstance> matches = this.Name.Binding.Matches
                    .Select(it =>
                    {
                        if (it.Instance.Target.IsFunction())
                            return it.Instance;
                        else if (it.Instance.Target is Property prop)
                            return prop.Getter?.InstanceOf?.TranslateThrough(it.Instance);
                        else
                            return null;
                    })
                    .Where(it => it != null);

                if (!matches.Any())
                {
                    this.resolution = new Option<CallResolution>(null);
                    if (!this.Callee.Evaluation.Components.IsJoker) // do not cascade errors
                        ctx.AddError(ErrorCode.NotFunctionType, this.Callee);
                }
                else
                {
                    if (this.DebugId== (20, 324))
                    {
                        ;
                    }
                    IEnumerable<CallResolution> targets = matches
                        .Select(it => CallResolution.Create(ctx, this.Name.TemplateArguments, this,
                            createCallContext(ctx, this.Name, it.TargetFunction), targetFunctionInstance: it))
                        .Where(it => it != null)
                        .StoreReadOnly();
                    targets = targets.Where(it => it.RequiredParametersUsed()).StoreReadOnly();
                    targets = targets.Where(it => it.CorrectlyFormedArguments()).StoreReadOnly();
                    targets = targets.Where(it => it.ArgumentTypesMatchParameters(ctx)).StoreReadOnly();
                    if (this.RequestedOutcomeTypeName != null)
                        targets = targets.Where(it => it.OutcomeMatchesRequest(ctx)).StoreReadOnly();

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

                        foreach (var group in this.Resolution.GetArgumentsMultipleTargeted())
                            // we only report second "override" because if there are more 
                            // it is more likely user forgot to mark parameter variadic
                            ctx.ErrorManager.AddError(ErrorCode.ArgumentForFunctionAlreadyGiven, group.Skip(1).FirstOrDefault());

                        foreach (FunctionParameter param in this.Resolution.GetUnfulfilledVariadicParameters())
                            ctx.ErrorManager.AddError(ErrorCode.InvalidNumberVariadicArguments, this, param);


                        if (targets.Count() == 1)
                        {
                            this.Resolution.EnhanceArguments(ctx);
                        }

                        this.Resolution.SetMappings(ctx);

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
                            NameReference this_name = this.Name;
                            this_name.DetachFrom(this);
                            this.callee = this_name.Recreate(this.Resolution.InferredTemplateArguments, 
                                this.Resolution.TargetFunctionInstance, this_name.Binding.Match.IsLocal);
                            this.callee.AttachTo(this);

                            this.Callee.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

                            if (!this.Name.Binding.HasMatch)
                                throw new Exception("We've just lost our binding, probably something wrong with template translations");
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
            }
        }

        private void ConvertToExplicitInvoke(ComputationContext ctx)
        {
            this.Callee.DetachFrom(this);
            this.callee = NameReference.Create(this.Callee, NameFactory.LambdaInvoke);
            this.callee.AttachTo(this);
            this.callee.Evaluated(ctx, EvaluationCall.AdHocCrossJump);
        }

        private static CallContext createCallContext(ComputationContext ctx, NameReference name, FunctionDefinition callTarget)
        {
            IExpression this_context = name.GetContext(callTarget);
            if (this_context == null)
                return new CallContext();

            this_context.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

            if (callTarget.Modifier.HasStatic && !callTarget.IsExtension)
                return new CallContext() { StaticContext = this_context.Evaluation.Components };
            else
                return new CallContext() { MetaThisArgument = FunctionArgument.Create(this_context) };
        }

        private static IEnumerable<CallResolution> resolveOverloading(IEnumerable<CallResolution> targets)
        {
            if (targets.Count() < 2)
                return targets;

            // the less, the better
            var arguments_matches = new List<Tuple<CallResolution, List<FunctionOverloadWeight>>>();
            foreach (CallResolution call_target in targets)
            {
                var weights = new List<FunctionOverloadWeight>();
                foreach (FunctionArgument arg in call_target.Arguments)
                {
                    FunctionParameter param = call_target.GetParamByArgIndex(arg.Index);
                    var weight = new FunctionOverloadWeight();
                    // prefer non-variadic parameters
                    if (param.IsVariadic)
                        weight.Penalty += 1;
                    // prefer concrete type over generic one (foo(Int) better than foo<T>(T))
                    // note we use untranslated param evaluation here to achieve this effect
                    if (!param.Evaluation.Components.IsExactlySame(arg.Evaluation.Components, jokerMatchesAll: true))
                        weight.Penalty += 2;

                    // prefer exact match instead more general match (Int->Int is better than Int->Object)
                    IEntityInstance param_trans_eval = call_target.GetTransParamEvalByArg(arg);
                    TypeMatch m = call_target.TypeMatches[arg.Index].Value;
                    if (m.HasFlag(TypeMatch.Substitute))
                    {
                        weight.Penalty += 4;
                        weight.SubstitutionDistance = m.Distance;
                    }
                    else if (!m.HasFlag(TypeMatch.Same)) // conversions
                        weight.Penalty += 8;

                    weights.Add(weight);
                }
                // bonus if optional parameters are explicitly targeted (i.e. default values are not used)
                weights.Add(new FunctionOverloadWeight(call_target.AllParametersUsed() ? 0 : 1));

                arguments_matches.Add(Tuple.Create(call_target, weights));
            }

            Option<Tuple<CallResolution, List<FunctionOverloadWeight>>> best = arguments_matches
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

        public bool IsRecall()
        {
            return isRecall(out FunctionDefinition dummy1, out FunctionDefinition dummy2);
        }
        private bool isRecall(out FunctionDefinition currentFunc, out FunctionDefinition targetFunc)
        {
            currentFunc = this.EnclosingScope<FunctionDefinition>();
            targetFunc = this.Name.Binding.Match.Instance.Target as FunctionDefinition;
            return currentFunc != null && targetFunc == currentFunc;
        }

        public FunctionCall ConvertIndexerIntoSetter(IExpression rhs)
        {
            this.OwnedNodes.ForEach(it => it.DetachFrom(this));
            NameReference idx_getter = this.Name;
            idx_getter.Prefix.DetachFrom(idx_getter);

            return new FunctionCall(CallMode.Indexer,
                NameReference.Create(this.Name.Prefix, NameFactory.PropertySetter),
                this.Arguments.Concat(FunctionArgument.Create(NameFactory.PropertySetterValueParameter, rhs)),
                requestedOutcomeType: null);
        }
    }
}
