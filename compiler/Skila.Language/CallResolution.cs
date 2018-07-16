using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Expressions;
using Skila.Language.Extensions;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed partial class CallResolution
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(CallResolution));
#endif

        internal static CallResolution Create(ComputationContext ctx,
            IEnumerable<TemplateArgument> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance)
        {
            // at this point we target true function (any function-like object is converted to closure already)
            if (!targetFunctionInstance.Target.IsFunction())
                throw new Exception("Internal error");

            List<int> arg_param_mapping = createArgParamMapping(targetFunctionInstance,
                callContext.MetaThisArgument, argumentsProvider.UserArguments);
            if (arg_param_mapping == null)
                return null;

            var result = new CallResolution(ctx, templateArguments, argumentsProvider,
                callContext,
                targetFunctionInstance,
                arg_param_mapping,
                out bool success);

            if (success)
                return result;
            else
                return null;
        }

        private const int noMapping = -1;

        public EntityInstance TargetFunctionInstance { get; }
        public FunctionDefinition TargetFunction => this.TargetFunctionInstance.TargetTemplate.CastFunction();

        // function arg -> function param indices (not template ones!)
        private readonly IReadOnlyList<int> argParamMapping;

        // reverse of the above
        private readonly IReadOnlyList<IEnumerable<int>> paramArgMapping;

        private readonly IReadOnlyList<ParameterType> translatedParamEvaluations;
        private readonly EvaluationInfo translatedResultEvaluation;
        private readonly IFunctionArgumentsProvider argumentsProvider;
        private readonly TypeMatch?[] typeMatches;
        public IReadOnlyList<TypeMatch?> TypeMatches => this.typeMatches;

        public IReadOnlyList<FunctionArgument> TrueArguments { get; }
        public IReadOnlyList<FunctionArgument> UserArguments => this.argumentsProvider.UserArguments;
        public IReadOnlyCollection<INameReference> InferredTemplateArguments { get; }
        private readonly IReadOnlyList<IEntityInstance> templateArguments;

        public FunctionArgument MetaThisArgument { get; } // null for regular functions (not-methods)
        public EvaluationInfo Evaluation => this.translatedResultEvaluation;

        public bool IsExtendedCall => this.TargetFunction.IsExtension && this.MetaThisArgument != null;

        private CallResolution(ComputationContext ctx,
            IEnumerable<TemplateArgument> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance,
            List<int> argParamMapping,
            out bool success)
        {
            success = true;

            this.MetaThisArgument = callContext.MetaThisArgument;
            this.TargetFunctionInstance = targetFunctionInstance;
            this.argumentsProvider = argumentsProvider;

            if (this.IsExtendedCall)
                this.TrueArguments = new[] { this.MetaThisArgument }.Concat(this.argumentsProvider.UserArguments).StoreReadOnlyList();
            else
                this.TrueArguments = this.argumentsProvider.UserArguments;

            this.typeMatches = new TypeMatch?[this.TargetFunction.Parameters.Count];
            this.templateArguments = (templateArguments.Any()
                ? templateArguments.Select(it => it.TypeName.Evaluation.Components)
                : this.TargetFunction.Name.Parameters.Select(it => EntityInstance.Joker)).StoreReadOnlyList();
            this.argParamMapping = argParamMapping;
            this.paramArgMapping = createParamArgMapping(this.argParamMapping);

            IEntityInstance call_ctx_eval = callContext.Evaluation;
            if (call_ctx_eval != null)
                // we need to have evaluation of the value, not ref/ptr, so the correct template translation table could kick in
                ctx.Env.Dereference(call_ctx_eval, out call_ctx_eval);

            if (this.DebugId==  (41, 745))
            {
                ;
            }
            extractParameters(ctx, argumentsProvider, call_ctx_eval, this.TargetFunctionInstance,
                out this.translatedParamEvaluations,
                out this.translatedResultEvaluation);

            if (this.templateArguments.Any(it => it.IsJoker))
            {
                if (this.DebugId == (40, 747))
                {
                    ;
                }
                if (argumentsProvider.DebugId == (20, 369))
                {
                    ;
                }
                IEnumerable<IEntityInstance> inferred = inferTemplateArgumentsFromExpressions(ctx);

                inferred = inferTemplateArgumentsFromConstraints(ctx, inferred);

                if (inferred.Any(it => it.IsJoker))
                    success = false;
                else
                {
                    this.InferredTemplateArguments = inferred.Select(it => it.NameOf).StoreReadOnly();

                    this.TargetFunctionInstance = this.TargetFunctionInstance.Build(inferred,
                        this.TargetFunctionInstance.OverrideMutability);

                    extractParameters(ctx, argumentsProvider, call_ctx_eval, this.TargetFunctionInstance,
                        out this.translatedParamEvaluations,
                        out this.translatedResultEvaluation);

                }
            }
        }

        private static List<int> createArgParamMapping(EntityInstance targetFunctionInstance,
            FunctionArgument metaThisArgument,
            IReadOnlyList<FunctionArgument> arguments)
        {
            FunctionDefinition target_function = targetFunctionInstance.Target.CastFunction();

            List<int> argParamMapping = Enumerable.Repeat(noMapping, arguments.Count).ToList();

            {
                int param_idx = target_function.IsExtension && metaThisArgument != null ? 1 : 0;
                foreach (FunctionArgument arg in arguments)
                {
                    FunctionParameter param;

                    if (arg.HasNameLabel)
                        param = target_function.Parameters.FirstOrDefault(it => it.Name.Name == arg.NameLabel);
                    else
                        param = param_idx < target_function.Parameters.Count ? target_function.Parameters[param_idx] : null;

                    if (param == null)
                        return null; // not matching for argument, so this is not the function we are looking for
                    else if (argParamMapping[arg.Index] != noMapping)
                        throw new Exception("Internal error");
                    else
                    {
                        argParamMapping[arg.Index] = param.Index;
                        // progress with indexing only if we are not hitting variadic parameter
                        param_idx = param.Index + (param.IsVariadic ? 0 : 1);
                    }
                }
            }

            return argParamMapping;
        }

        private static IReadOnlyList<IEnumerable<int>> createParamArgMapping(IReadOnlyCollection<int> argParamMapping)
        {
            if (!argParamMapping.Any())
                return Enumerable.Empty<IEnumerable<int>>().StoreReadOnlyList();

            int max = argParamMapping.Max();
            var param_arg_mapping = Enumerable.Range(0, max + 1).Select(_ => new List<int>()).ToList();
            int arg_idx = 0;
            foreach (int param_idx in argParamMapping)
            {
                if (param_idx != noMapping)
                    param_arg_mapping[param_idx].Add(arg_idx);
                ++arg_idx;
            }

            return param_arg_mapping;
        }

        internal void SetMappings(ComputationContext ctx)
        {
            foreach (var arg in this.TrueArguments)
                arg.SetTargetParam(ctx, this.GetParamByArg(arg));
        }

        private static void extractParameters(ComputationContext ctx,
            INode callNode,
            IEntityInstance objectInstance,
            EntityInstance targetFunctionInstance,
            out IReadOnlyList<ParameterType> translatedParamEvaluations,
            out EvaluationInfo translatedResultEvaluation)
        {
            FunctionDefinition target_function = targetFunctionInstance.TargetTemplate.CastFunction();

            translatedParamEvaluations = target_function.Parameters
                .Select(it => ParameterType.Create(it, objectInstance, targetFunctionInstance))
                .StoreReadOnlyList();

            target_function.ResultTypeName.Evaluated(ctx, EvaluationCall.AdHocCrossJump);

            IEntityInstance components = translateFunctionElement(target_function.ResultTypeName.Evaluation.Components,
                objectInstance, targetFunctionInstance);

            EntityInstance aggregate = translateFunctionElement(target_function.ResultTypeName.Evaluation.Aggregate,
                objectInstance, targetFunctionInstance);

            translatedResultEvaluation = new EvaluationInfo( components, aggregate);
        }

        // translate eval of parameter or result type
        private static T translateFunctionElement<T>(T functionElementTypeInstance, IEntityInstance objectInstance,
            EntityInstance targetFunctionInstance)
            where T : IEntityInstance
        {
            functionElementTypeInstance = functionElementTypeInstance.TranslateThrough(targetFunctionInstance);
            functionElementTypeInstance = functionElementTypeInstance.TranslateThrough(objectInstance);
            return functionElementTypeInstance;
        }

        internal bool ArgumentTypesMatchParameters(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.TrueArguments)
            {
                IEntityInstance param_eval = this.GetTransParamEvalByArg(arg);

                TypeMatch match = arg.Evaluation.Components.MatchesTarget(ctx, param_eval,
                    TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false));

                int idx = this.GetParamByArg(arg).Index;
                // in case of variadic parameter several arguments hit the same param, so their type matching can be different
                if (!this.typeMatches[idx].HasValue)
                    this.typeMatches[idx] = match;
                else if (this.typeMatches[idx].Value != match)
                    throw new NotImplementedException();

                    if (match.IsMismatch())
                        return false;

                if (match == TypeMatch.ImplicitReference)
                {
                    ;
                }
            }

            return true;
        }
        internal bool OutcomeMatchesRequest(ComputationContext ctx)
        {
            // requested result type has to match perfectly (without conversions)
            TypeMatch match = this.argumentsProvider.RequestedOutcomeTypeName.Evaluation.Components
                .MatchesTarget(ctx, this.translatedResultEvaluation.Components, TypeMatching.Create(ctx.Env.Options.InterfaceDuckTyping, allowSlicing: false));
            return match == TypeMatch.Same || match == TypeMatch.Substitute;
        }

        internal void EnhanceArguments(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.TrueArguments)
            {
                IEntityInstance param_eval = this.GetTransParamEvalByArg(arg);
                arg.DataTransfer(ctx, param_eval);
            }
        }

        public IEntityInstance GetTransParamEvalByArg(FunctionArgument arg)
        {
            ParameterType type = this.translatedParamEvaluations[GetParamByArg(arg).Index];
            return arg.IsSpread ? type.TypeInstance : type.ElementTypeInstance;
        }
        public FunctionParameter GetParamByArg(FunctionArgument arg)
        {
            if (arg == this.MetaThisArgument)
                return this.TargetFunction.Parameters[0];
            else
                return this.TargetFunction.Parameters[this.argParamMapping[arg.Index]];
        }

        public IEnumerable<FunctionArgument> GetArguments(int paramIndex)
        {
            if (this.DebugId == (36, 667))
            {
                ;
            }
            if (paramIndex >= this.paramArgMapping.Count)
                return Enumerable.Empty<FunctionArgument>();
            else if (paramIndex == 0 && this.IsExtendedCall)
                return new[] { this.MetaThisArgument };
            else
                return this.paramArgMapping[paramIndex].Select(arg_idx => this.UserArguments[arg_idx]);
        }

        private bool isParameterUsed(int paramIndex)
        {
            if (this.IsExtendedCall && paramIndex == 0)
                return true;
            else
            {
                FunctionParameter param = this.TargetFunction.Parameters[paramIndex];
                return this.GetArguments(paramIndex).Any();
            }
        }

        internal IEnumerable<IEnumerable<FunctionArgument>> GetArgumentsMultipleTargeted()
        {
            return this.TargetFunction.Parameters
                .Where(param => !param.IsVariadic)
                .Select(param => GetArguments(param.Index))
                .Where(indices => indices.Count() > 1);
        }

        internal IEnumerable<FunctionParameter> GetUnfulfilledVariadicParameters()
        {
            foreach (FunctionParameter param in this.TargetFunction.Parameters.Where(param => param.IsVariadic))
            {
                IEnumerable<FunctionArgument> arguments = GetArguments(param.Index);
                if (arguments.Any(it => it.IsSpread))
                    continue;

                if (!param.Variadic.IsWithinLimits(arguments.Count()))
                    yield return param;
            }
        }

        public bool CorrectlyFormedArguments()
        {
            foreach (FunctionParameter param in this.TargetFunction.Parameters)
            {
                IEnumerable<FunctionArgument> arguments = GetArguments(param.Index);
                int spreads = arguments.Count(it => it.IsSpread);

                if (param.IsVariadic)
                {
                    int direct = arguments.Any(it => !it.IsSpread) ? 1 : 0;

                    // you can have only single spread OR multiple directs, any other combination is invalid
                    if (spreads + direct > 1)
                        return false;
                }
                else if (spreads > 0)
                {
                    return false;
                }
            }

            return true;
        }


        public bool AllParametersUsed()
        {
            bool result = this.TargetFunction.Parameters.All(it => isParameterUsed(it.Index));
            return result;
        }

        public bool RequiredParametersUsed()
        {
            IEnumerable<FunctionParameter> left = this.TargetFunction.Parameters
                .Where(it => !it.IsOptional)
                .Where(it => !isParameterUsed(it.Index));
            return !left.Any();
        }

        private IEnumerable<IEntityInstance> inferTemplateArgumentsFromExpressions(ComputationContext ctx)
        {
            // regular inferrence
            IReadOnlyList<IEntityInstance> inferred = InferTemplateArguments(ctx, this.TargetFunction)
                .Select(it => it?.Instance)
                .StoreReadOnlyList();

            for (int i = 0; i < this.TargetFunctionInstance.TargetTemplate.Name.Parameters.Count; ++i)
                if (this.templateArguments[i].IsJoker)
                    yield return inferred[i] ?? EntityInstance.Joker;
                else
                    yield return templateArguments[i];
        }

        public IEnumerable<TimedIEntityInstance> InferTemplateArguments(ComputationContext ctx, TemplateDefinition template)
        {
            if (this.DebugId == (39, 14))
            {
                ;
            }

            // we try to cross-infer parameters when trying to match constructor (function) arguments with typedef parameters
            // new Foo<X>(arg)
            // Foo<X> here is typedef, not a function, the above unfolds into
            // t = alloc<Foo<X>>(); t.init(arg); ...
            // it is possible because constructor does not have any template parameters (like in C#)
            bool cross_parameters = template != this.TargetFunction;

            IReadOnlyList<TemplateParameter> template_parameters = template.Name.Parameters;

            var template_param_inference = new Dictionary<TemplateParameter, TimedIEntityInstance>();
            for (int i = 0; i < template_parameters.Count; ++i)
                template_param_inference.Add(template_parameters[i], null);

            foreach (FunctionArgument arg in this.TrueArguments)
            {
                IEntityInstance function_param_type = cross_parameters ? this.GetParamByArg(arg).Evaluation.Components : this.GetTransParamEvalByArg(arg);

                IEnumerable<Tuple<TemplateParameter, TimedIEntityInstance>> type_mapping
                    = extractTypeParametersMapping(ctx, template, arg.Evaluation.Aggregate.Lifetime, arg.Evaluation.Components,
                    function_param_type);

                foreach (Tuple<TemplateParameter, TimedIEntityInstance> pair in type_mapping)
                {
                    if (template_param_inference[pair.Item1] == null)
                        template_param_inference[pair.Item1] = pair.Item2;
                    else
                    {
                        if (!TypeMatcher.LowestCommonAncestor(ctx, template_param_inference[pair.Item1].Instance, pair.Item2.Instance,
                            out IEntityInstance common))
                            throw new NotImplementedException();

                        template_param_inference[pair.Item1] = TimedIEntityInstance.Create(
                            template_param_inference[pair.Item1].Lifetime.Shorter(pair.Item2.Lifetime),
                            common);
                    }

                }
            }

            return template_parameters.Select(it => template_param_inference[it]);
        }

        private IEnumerable<IEntityInstance> inferTemplateArgumentsFromConstraints(ComputationContext ctx,
            IEnumerable<IEntityInstance> templateArguments)
        {
            if (templateArguments.All(it => !it.IsJoker))
                return templateArguments;

            EntityInstance closedTemplate = this.TargetFunctionInstance.Build(templateArguments,
                this.TargetFunctionInstance.OverrideMutability);

            var result = new List<IEntityInstance>();
            int index = -1;
            foreach (IEntityInstance arg in templateArguments)
            {
                ++index;
                if (!arg.IsJoker)
                {
                    result.Add(arg);
                    continue;
                }

                TemplateParameter param = this.TargetFunctionInstance.TargetTemplate.Name.Parameters[index];

                IEntityInstance computed = null;
                foreach (EntityInstance base_of in param.Constraint.TranslateBaseOf(closedTemplate))
                {
                    if (computed == null)
                        computed = base_of;
                    else
                    {
                        if (TypeMatcher.LowestCommonAncestor(ctx, computed, base_of, out IEntityInstance lca))
                            computed = lca;
                        else
                        {
                            computed = null;
                            break;
                        }
                    }
                }

                result.Add(computed ?? EntityInstance.Joker);
            }

            return result;
        }

        private static IEnumerable<Tuple<TemplateParameter, TimedIEntityInstance>> extractTypeParametersMapping(ComputationContext ctx,
            TemplateDefinition template,
            Lifetime argLifetime,
            IEntityInstance argType, IEntityInstance paramType)
        {

            // three steps logic of dereferencing here
            // (1) we cannot pass references as template arguments, so we dereference the type right away
            bool dereferencing_arg = ctx.Env.IsReferenceOfType(argType);
            if (dereferencing_arg)
                ctx.Env.DereferencedOnce(argType, out argType, out bool dummy);

            // (2) we need to drop reference from param type as well (scenario: passing values to function taking references of them)
            if (ctx.Env.IsReferenceOfType(paramType))
            {
                ctx.Env.DereferencedOnce(paramType, out paramType, out bool dummy1);

                // (3) at this point it could happen that we didn't dereferenced pointer argument, so we do it, 
                // but only if the argument was not dereferenced already
                if (!dereferencing_arg && ctx.Env.IsPointerOfType(argType))
                    ctx.Env.DereferencedOnce(argType, out argType, out bool dummy2);
            }
            // done, we covered:
            // value -> ref
            // ref -> ref
            // ptr -> ref


            // let's say we pass Tuple<Int,String> into function which expects there Tuple<K,V>
            // we try to extract those type to return mapping K -> Int, V -> String
            // please note that those mappings can repeat, like T -> Int, T -> String 
            // thus we return just enumerable, not dictionary

            var param_type_instance = paramType as EntityInstance;
            if (param_type_instance == null)
                return Enumerable.Empty<Tuple<TemplateParameter, TimedIEntityInstance>>();

            // case when we have direct hit, function: def foo<T>(x T)
            // and we have argument (for example) Int against parameter "x T", thus we simply match them: T=Int
            if (param_type_instance.IsTemplateParameterOf(template))
                return new[] { Tuple.Create(param_type_instance.TargetType.TemplateParameter,
                    TimedIEntityInstance.Create( argLifetime,argType)) };
            else
            {
                var arg_type_instance = argType as EntityInstance;
                if (arg_type_instance == null)
                    return Enumerable.Empty<Tuple<TemplateParameter, TimedIEntityInstance>>();

                IEnumerable<EntityInstance> arg_family = arg_type_instance.Inheritance(ctx).OrderedAncestorsWithoutObject
                    .Concat(arg_type_instance);
                arg_type_instance = arg_family
                    .SingleOrDefault(it => it.TargetType == param_type_instance.TargetType);

                if (arg_type_instance == null)
                    return Enumerable.Empty<Tuple<TemplateParameter, TimedIEntityInstance>>();

                var zipped = arg_type_instance.TemplateArguments.SyncZip(param_type_instance.TemplateArguments);
                return zipped
                    .Select(it => extractTypeParametersMapping(ctx, template, argLifetime, it.Item1, it.Item2))
                    .Flatten()
                    .ToArray();
            }
        }

        public override string ToString()
        {
            return "(" + this.TrueArguments.Select(it => $"{it}").Join(",") + ") -> "
                + $"{this.TargetFunctionInstance}(" + this.TargetFunction.Parameters.Select(it => $"{it}").Join(",") + ")";
        }

    }
}
