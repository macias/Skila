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
        public DebugId DebugId { get; } = new DebugId();
#endif

        internal static CallResolution Create(ComputationContext ctx,
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance)
        {
            // at this point we target true function (any function-like object is converted to closure already)
            if (!targetFunctionInstance.Target.IsFunction())
                throw new Exception("Internal error");


            FunctionDefinition target_function = targetFunctionInstance.Target.CastFunction();

            var arg_param_mapping = new List<int>(capacity: argumentsProvider.Arguments.Count);
            int param_idx = 0;
            foreach (FunctionArgument arg in argumentsProvider.Arguments)
            {
                FunctionParameter param;

                if (arg.HasNameLabel)
                    param = target_function.Parameters.FirstOrDefault(it => it.Name.Name == arg.NameLabel);
                else
                    param = param_idx < target_function.Parameters.Count ? target_function.Parameters[param_idx] : null;

                if (param == null)
                    return null; // not matching for argument, so this is not the function we are looking for
                else
                {
                    arg_param_mapping.Add(param.Index);
                    // progress with indexing only if we are not hitting variadic parameter
                    param_idx = param.Index + (param.IsVariadic ? 0 : 1);
                }
            }


            return new CallResolution(ctx, templateArguments, argumentsProvider,
                callContext,
                targetFunctionInstance,
                arg_param_mapping);
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

        public IReadOnlyList<FunctionArgument> Arguments => this.argumentsProvider.Arguments;
        public IReadOnlyCollection<INameReference> InferredTemplateArguments { get; }
        private readonly IReadOnlyCollection<INameReference> templateArguments;

        public FunctionArgument MetaThisArgument { get; private set; } // null for regular functions (not-methods)
        public EvaluationInfo Evaluation => this.translatedResultEvaluation;

        private CallResolution(ComputationContext ctx,
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance,
            IReadOnlyList<int> argParamMapping)
        {
            this.MetaThisArgument = callContext.MetaThisArgument;
            this.TargetFunctionInstance = targetFunctionInstance;
            this.argumentsProvider = argumentsProvider;
            this.typeMatches = new TypeMatch?[this.TargetFunction.Parameters.Count];
            this.templateArguments = templateArguments.StoreReadOnly();
            this.argParamMapping = argParamMapping;

            if (this.DebugId.Id == 5691)
            {
                ;
            }


            extractParameters(ctx, callContext.Evaluation, this.TargetFunctionInstance,
                out this.translatedParamEvaluations,
                out this.translatedResultEvaluation);


            // compute param->arg mapping
            if (!this.argParamMapping.Any())
                this.paramArgMapping = Enumerable.Empty<IEnumerable<int>>().StoreReadOnlyList();
            else
            {
                int max = this.argParamMapping.Max();
                var param_arg_mapping = Enumerable.Range(0, max + 1).Select(_ => new List<int>()).ToList();
                int arg_idx = 0;
                foreach (int param_idx in argParamMapping)
                {
                    if (param_idx != noMapping)
                        param_arg_mapping[param_idx].Add(arg_idx);
                    ++arg_idx;
                }
                this.paramArgMapping = param_arg_mapping;
            }

            this.InferredTemplateArguments = inferTemplateArguments(ctx);

            if (this.InferredTemplateArguments != null)
            {
                this.TargetFunctionInstance = EntityInstance.Create(ctx, this.TargetFunctionInstance, this.InferredTemplateArguments,
                     overrideMutability: false);
                extractParameters(ctx, callContext.Evaluation, this.TargetFunctionInstance,
                    out this.translatedParamEvaluations,
                    out this.translatedResultEvaluation);
            }
        }

        internal void SetMappings(ComputationContext ctx)
        {
            foreach (var arg in this.Arguments)
                arg.SetTargetParam(ctx, this.GetParamByArgIndex(arg.Index));
        }

        private static void extractParameters(ComputationContext ctx,
            IEntityInstance objectType,
            EntityInstance targetFunctionInstance,
            out IReadOnlyList<ParameterType> translatedParamEvaluations,
            out EvaluationInfo translatedResultEvaluation)
        {
            FunctionDefinition target_function = targetFunctionInstance.TargetTemplate.CastFunction();

            translatedParamEvaluations = target_function.Parameters
                .Select(it => ParameterType.Create(it, objectType, targetFunctionInstance))
                .StoreReadOnlyList();

            target_function.ResultTypeName.Evaluated(ctx);

            IEntityInstance components = target_function.ResultTypeName.Evaluation.Components;
            components = components.TranslateThrough(targetFunctionInstance);
            components = components.TranslateThrough(objectType);

            EntityInstance aggregate = target_function.ResultTypeName.Evaluation.Aggregate;
            aggregate = aggregate.TranslateThrough(targetFunctionInstance);
            aggregate = aggregate.TranslateThrough(objectType);

            translatedResultEvaluation = new EvaluationInfo(components, aggregate);
        }

        internal bool ArgumentTypesMatchParameters(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.Arguments)
            {
                if (arg.DebugId.Id == 8886)
                {
                    ;
                }
                IEntityInstance param_eval = this.GetTransParamEvalByArg(arg);

                TypeMatch match = arg.Evaluation.Components.MatchesTarget(ctx, param_eval, allowSlicing: false);

                int idx = this.argParamMapping[arg.Index];
                // in case of variadic parameter several arguments hit the same param, so their type matching can be different
                if (!this.typeMatches[idx].HasValue)
                    this.typeMatches[idx] = match;
                else if (this.typeMatches[idx].Value != match)
                    throw new NotImplementedException();

                if (match == TypeMatch.No)
                    return false;
            }

            return true;
        }
        internal bool OutcomeMatchesRequest(ComputationContext ctx)
        {
            // requested result type has to match perfectly (without conversions)
            TypeMatch match = this.argumentsProvider.RequestedOutcomeTypeName.Evaluation.Components
                .MatchesTarget(ctx, this.translatedResultEvaluation.Components, allowSlicing: false);
            return match == TypeMatch.Same || match == TypeMatch.Substitute;
        }

        internal void EnhanceArguments(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.Arguments)
            {
                IEntityInstance param_eval = this.GetTransParamEvalByArg(arg);
                arg.DataTransfer(ctx, param_eval);
            }
        }

        public IEntityInstance GetTransParamEvalByArg(FunctionArgument arg)
        {
            ParameterType type = this.translatedParamEvaluations[this.argParamMapping[arg.Index]];
            return arg.IsSpread ? type.TypeInstance : type.ElementTypeInstance;
        }
        public FunctionParameter GetParamByArgIndex(int argIndex)
        {
            return this.TargetFunction.Parameters[this.argParamMapping[argIndex]];
        }

        private IEnumerable<FunctionArgument> getArguments(int paramIndex)
        {
            if (paramIndex >= this.paramArgMapping.Count)
                return Enumerable.Empty<FunctionArgument>();
            else
                return this.paramArgMapping[paramIndex].Select(arg_idx => this.Arguments[arg_idx]);
        }

        private bool isParameterUsed(int paramIndex)
        {
            return this.getArguments(paramIndex).Any();
        }

        internal IEnumerable<IEnumerable<FunctionArgument>> GetArgumentsMultipleTargeted()
        {
            return this.TargetFunction.Parameters
                .Where(param => !param.IsVariadic)
                .Select(param => getArguments(param.Index))
                .Where(indices => indices.Count() > 1);
        }

        internal IEnumerable<FunctionParameter> GetUnfulfilledVariadicParameters()
        {
            foreach (FunctionParameter param in this.TargetFunction.Parameters.Where(param => param.IsVariadic))
            {
                IEnumerable<FunctionArgument> arguments = getArguments(param.Index);
                if (arguments.Any(it => it.IsSpread))
                    continue;

                if (!param.Variadic.IsWithinLimits(arguments.Count()))
                    yield return param;
            }
        }

        public bool AllParametersUsed()
        {
            bool result = this.TargetFunction.Parameters.All(it => isParameterUsed(it.Index));
            return result;
        }

        public bool RequiredParametersUsed()
        {
            bool result = this.TargetFunction.Parameters.Where(it => !it.IsOptional).All(it => isParameterUsed(it.Index));
            return result;
        }

        private IReadOnlyCollection<INameReference> inferTemplateArguments(ComputationContext ctx)
        {
            if (this.DebugId.Id == 11172)
            {
                ;
            }
            if (!this.TargetFunctionInstance.MissingTemplateArguments)
                return null;

            IReadOnlyList<TemplateParameter> template_parameters
                = this.TargetFunctionInstance.TargetTemplate.Name.Parameters.StoreReadOnlyList();

            var template_param_inference = new Dictionary<TemplateParameter, Tuple<IEntityInstance, bool>>();
            for (int i = 0; i < template_parameters.Count; ++i)
                if (this.templateArguments.Any())
                    template_param_inference.Add(template_parameters[i], Tuple.Create(templateArguments.ElementAt(i).Evaluation.Components, true));
                else
                    template_param_inference.Add(template_parameters[i], Tuple.Create((IEntityInstance)null, false));


            foreach (FunctionArgument arg in this.Arguments)
            {
                IEntityInstance function_param_type = this.GetTransParamEvalByArg(arg);

                IEnumerable<Tuple<TemplateParameter, IEntityInstance>> type_mapping
                    = extractTypeParametersMapping(this.TargetFunction, arg.Evaluation.Components, function_param_type);

                foreach (var pair in type_mapping)
                    if (!template_param_inference[pair.Item1].Item2)
                    {
                        if (template_param_inference[pair.Item1].Item1 == null)
                            template_param_inference[pair.Item1] = Tuple.Create(pair.Item2, false);
                        else
                        {
                            if (!TypeMatcher.LowestCommonAncestor(ctx, template_param_inference[pair.Item1].Item1, pair.Item2,
                                out IEntityInstance common))
                                throw new NotImplementedException();
                            template_param_inference[pair.Item1] = Tuple.Create(common, false);
                        }
                    }

            }

            // filling missing types with jokers
            foreach (KeyValuePair<TemplateParameter, Tuple<IEntityInstance, bool>> infer_pair in template_param_inference)
                if (infer_pair.Value.Item1 == null)
                    template_param_inference[infer_pair.Key] = new Tuple<IEntityInstance, bool>(EntityInstance.Joker, false);

            return template_parameters.Select(it => template_param_inference[it].Item1.NameOf).StoreReadOnly();
        }

        private static IEnumerable<Tuple<TemplateParameter, IEntityInstance>> extractTypeParametersMapping(FunctionDefinition function,
            IEntityInstance argType, IEntityInstance paramType)
        {
            // let's say we pass Tuple<Int,String> into function which expects there Tuple<K,V>
            // we try to extract those type to return mapping K -> Int, V -> String
            // please note that those mappings can repeat, like T -> Int, T -> String 
            // thus we return just enumerable, not dictionary

            var param_type_instance = paramType as EntityInstance;
            if (param_type_instance == null)
                return Enumerable.Empty<Tuple<TemplateParameter, IEntityInstance>>();

            if (param_type_instance.IsTemplateParameterOf(function))
                return new[] { Tuple.Create(param_type_instance.TargetType.TemplateParameter, argType) };
            else
            {
                var arg_type_instance = argType as EntityInstance;
                if (arg_type_instance == null || arg_type_instance.TemplateArguments.Count != param_type_instance.TemplateArguments.Count)
                    return Enumerable.Empty<Tuple<TemplateParameter, IEntityInstance>>();

                var zipped = arg_type_instance.TemplateArguments.SyncZip(param_type_instance.TemplateArguments);
                return zipped
                    .Select(it => extractTypeParametersMapping(function, it.Item1, it.Item2))
                    .Flatten()
                    .ToArray();
            }
        }

        public override string ToString()
        {
            return "(" + this.Arguments.Select(it => it.ToString()).Join(",") + ") -> "
                + this.TargetFunctionInstance.ToString() + "(" + this.TargetFunction.Parameters.Select(it => it.ToString()).Join(",") + ")";
        }

    }
}
