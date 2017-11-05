﻿using System;
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
    public sealed class FunctionCall : Node, IExpression, IFunctionArgumentsProvider
    {
        public static FunctionCall Create(NameReference name, params FunctionArgument[] arguments)
        {
            return new FunctionCall(name, arguments, requestedOutcomeType: null);
        }
        public static FunctionCall CreateToCall(IExpression expr, NameReference typeName)
        {
            return new FunctionCall(NameReference.Create(expr, NameFactory.ConvertFunctionName),
                arguments: null, requestedOutcomeType: typeName);
        }

        private bool? isRead;
        public bool IsRead { get { return this.isRead.Value; } set { if (this.isRead.HasValue) throw new Exception("Internal error"); this.isRead = value; } }

        private Option<CallResolution> resolution;
        public CallResolution Resolution => this.resolution.Value;
        public bool IsComputed => this.Evaluation != null;
        public IEntityInstance Evaluation { get; private set; }
        public ValidationData Validation { get; set; }
        public bool IsDereferenced { get; set; }
        // public bool IsStaticCall => this.Resolution.TargetInstance.Target.Modifier.HasStatic;

        public NameReference Name { get; private set; }
        public IReadOnlyList<FunctionArgument> Arguments { get; }
        public override IEnumerable<INode> OwnedNodes => Arguments.Select(it => it.Cast<INode>()).Concat(Name)
            .Concat(RequestedOutcomeTypeName).Where(it => it != null);
        public ExecutionFlow Flow => ExecutionFlow.CreatePath(Arguments);

        public INameReference RequestedOutcomeTypeName { get; }

        public ExpressionReadMode ReadMode => this.Resolution?.CallMode ?? ExpressionReadMode.OptionalUse;

        private FunctionCall(NameReference name, IEnumerable<FunctionArgument> arguments, NameReference requestedOutcomeType)
          : base()
        {
            this.Name = name;
            this.Arguments = (arguments ?? Enumerable.Empty<FunctionArgument>()).Indexed().StoreReadOnlyList();
            this.RequestedOutcomeTypeName = requestedOutcomeType;

            this.OwnedNodes.ForEach(it => it.AttachTo(this));
        }
        public override string ToString()
        {
            return this.Name + "(" + Arguments.Select(it => it.ToString()).Join(",") + ")";
        }

        public void Validate(ComputationContext ctx)
        {
        }

        public void Evaluate(ComputationContext ctx)
        {
            if (this.Evaluation == null)
            {
                if (this.DebugId.Id == 8785)
                {
                    ;
                }

                EntityInstance eval = this.Name.Evaluated(ctx).Cast<EntityInstance>();

                this.Arguments.ForEach(it => it.Evaluate(ctx));

                if (ctx.Env.IsFunctionType(eval))
                {
                    IEnumerable<CallResolution> targets = this.Name.Binding.Matches
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
                            ctx.ErrorManager.AddError(ErrorCode.NOTEST_AmbiguousOverloadedCall, this, targets.Select(it => it.TargetInstance.Target));
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
                            // leave only binding which was used for mapping
                            this.Name.Binding.Filter(it => it == this.Resolution.BindingMatch);
                        else
                        {
                            // var name = this.Name;
                            this.Name = this.Name.Recreate(this.Resolution.InferredTemplateArguments);

                            eval = this.Name.Evaluated(ctx) as EntityInstance;
                            //    this.Name = name;

                            this.Name.Binding.Filter(it => it == this.Resolution.TargetInstance);
                        }

                        if (this.DebugId.Id == 228)
                        {
                            ;
                        }

                        this.Evaluation = eval.TemplateArguments.Last();
                    }
                }
                else
                {
                    this.resolution = new Option<CallResolution>(null);
                    ctx.AddError(ErrorCode.NotFunctionType, this.Name);
                }


                if (this.Evaluation == null)
                    this.Evaluation = EntityInstance.Joker;

                foreach (IExpression arg in Arguments)
                    arg.ValidateValueExpression(ctx);

                if (this.Resolution != null)
                {
                    foreach (var group in this.Resolution.GetArgumentsMultipleTargeted())
                        // we only report second "override" because if there are more it is more likely user forgot to mark parameter variadic
                        ctx.ErrorManager.AddError(ErrorCode.ArgumentForFunctionAlreadyGiven, group.Skip(1).FirstOrDefault());

                    foreach (FunctionParameter param in this.Resolution.GetUnfulfilledVariadicParameters())
                        ctx.ErrorManager.AddError(ErrorCode.InvalidNumberVariadicArguments, this, param);
                }
            }
        }

        private static CallContext createCallContext(ComputationContext ctx, NameReference name, IEntity callTarget)
        {
            IExpression this_context = name.GetContext(callTarget);
            if (this_context == null)
                return new CallContext();

            this_context.Evaluated(ctx);

            if (callTarget.Modifier.HasStatic)
                return new CallContext() { StaticContext = this_context.Evaluation };
            else
                return new CallContext() { MetaThisArgument = FunctionArgument.Create(this_context) };
        }

        private static IEnumerable<CallResolution> resolveOverloading(IEnumerable<CallResolution> targets)
        {
            if (targets.Count() < 2)
                return targets;

            // the less, the better
            var arguments_matches = new List<Tuple<CallResolution, List<int>>>();
            foreach (CallResolution target in targets)
            {
                List<int> matches = target.Arguments
                    .Select(it =>
                    {
                        FunctionParameter param = target.GetParamByArgIndex(it.Index);
                        int weight = 0;
                        // prefer non-variadic parameters
                        if (param.IsVariadic)
                            weight += 1;
                        // prefer concrete type over generic one (foo(Int) better than foo<T>(T))
                        // note we use untranslated param evaluation here to achieve this effect
                        if (!param.Evaluation.IsSame(it.Evaluation, jokerMatchesAll: true))
                            weight += 2;
                        // prefer exact match instead more general match (Int->Int is better than Int->Object)
                        if (!target.GetTransParamEvalByArgIndex(it.Index).IsSame(it.Evaluation, jokerMatchesAll: true))
                            weight += 4;
                        return weight;
                    })
                    .ToList();
                // bonus if optional parameters are explicitly targeted (i.e. default values are not used)
                matches.Add(target.AllParametersUsed() ? 0 : 1);

                arguments_matches.Add(Tuple.Create(target, matches));
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
            return !this.Evaluation.IsValueType(ctx);
        }
    }
}
