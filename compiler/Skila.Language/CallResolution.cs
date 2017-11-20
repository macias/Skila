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
    public sealed class CallResolution
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        internal static CallResolution Create(ComputationContext ctx,
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetInstance)
        {
            // at this point we target true function (any function-like object is converted to closure already)
            if (!targetInstance.Target.IsFunction())
                throw new Exception("Internal error");

            return new CallResolution(ctx, templateArguments, argumentsProvider,
                    callContext,
                    targetFunctionInstance: targetInstance);
        }

        private const int noMapping = -1;

        public EntityInstance TargetFunctionInstance { get; }
        public ExpressionReadMode CallMode => this.directlyTargetsFunction ? this.TargetFunctionInstance.TargetTemplate.CastFunction().CallMode : ExpressionReadMode.OptionalUse;

        // function arg -> function param indices (not template ones!)
        private readonly IReadOnlyList<int> argParamMapping;

        // reverse of the above
        private readonly IReadOnlyList<IEnumerable<int>> paramArgMapping;

        private readonly IReadOnlyList<IEntityInstance> translatedParamEvaluations;
        private readonly IEntityInstance translatedResultEvaluation;
        private FunctionDefinition signature => this.TargetFunctionInstance.TargetTemplate.CastFunction();
        private readonly IFunctionArgumentsProvider argumentsProvider;
        public IReadOnlyList<FunctionArgument> Arguments => this.argumentsProvider.Arguments;
        public IReadOnlyCollection<INameReference> InferredTemplateArguments { get; }
        private readonly IReadOnlyCollection<INameReference> templateArguments;
        private bool directlyTargetsFunction => !this.TargetFunctionInstance.TargetTemplate.IsType();

        public FunctionArgument MetaThisArgument { get; private set; } // null for regular functions (not-methods)
        public IEntityInstance Evaluation => this.translatedResultEvaluation;

        private CallResolution(ComputationContext ctx,
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance)
        {
            if (this.DebugId.Id == 11109)
            {
                ;
            }
            this.MetaThisArgument = callContext.MetaThisArgument;
            this.TargetFunctionInstance = targetFunctionInstance;
            this.argumentsProvider = argumentsProvider;
            this.templateArguments = templateArguments.StoreReadOnly();

            extractParameters(ctx, callContext.Evaluation, this.TargetFunctionInstance,
                this.signature,
                out this.translatedParamEvaluations, out this.translatedResultEvaluation);

            {
                var arg_param_mapping = new List<int>();
                int param_idx = 0;
                foreach (FunctionArgument arg in this.Arguments)
                {
                    FunctionParameter param;

                    if (arg.HasNameLabel)
                        param = this.signature.Parameters.FirstOrDefault(it => it.Name.Name == arg.NameLabel);
                    else
                        param = param_idx < this.signature.Parameters.Count ? this.signature.Parameters[param_idx] : null;

                    if (param == null)
                        arg_param_mapping.Add(noMapping);
                    else
                    {
                        arg_param_mapping.Add(param.Index);
                        // progress with indexing only if we are not hitting variadic parameter
                        param_idx = param.Index + (param.IsVariadic ? 0 : 1);
                    }
                }

                this.argParamMapping = arg_param_mapping;
            }

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
                this.TargetFunctionInstance = this.TargetFunctionInstance.Target.GetInstanceOf(this.InferredTemplateArguments
                    .Select(it => it.Evaluated(ctx)));
                extractParameters(ctx, callContext.Evaluation, this.TargetFunctionInstance,
                    this.signature,
                    out this.translatedParamEvaluations, out this.translatedResultEvaluation);
            }
        }

        internal void SetMappings()
        {
            foreach (var arg in this.Arguments)
                arg.MappedTo = this.GetParamByArgIndex(arg.Index);
        }

        private static void extractParameters(ComputationContext ctx,
            IEntityInstance objectInstance,
            EntityInstance targetFunctionInstance,
            FunctionDefinition signature,
            out IReadOnlyList<IEntityInstance> translatedParamEvaluations,
            out IEntityInstance translatedResultEvaluation)
        {
            translatedParamEvaluations = signature.Parameters
                .Select(it => it.Evaluated(ctx).TranslateThrough(objectInstance).TranslateThrough(targetFunctionInstance))
                .StoreReadOnlyList();

            translatedResultEvaluation = signature.ResultTypeName.Evaluated(ctx)
                .TranslateThrough(objectInstance).TranslateThrough(targetFunctionInstance);
        }

        internal bool ArgumentTypesMatchParameters(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.Arguments)
            {
                if (arg.DebugId.Id == 8886)
                {
                    ;
                }
                IEntityInstance param_eval = this.GetTransParamEvalByArgIndex(arg.Index);
                TypeMatch match = arg.Evaluation.MatchesTarget(ctx, param_eval, allowSlicing: false);
                if (match == TypeMatch.No)
                    return false;
            }

            return true;
        }
        internal bool OutcomeMatchesRequest(ComputationContext ctx)
        {
            // requested result type has to match perfectly (without conversions)
            return this.argumentsProvider.RequestedOutcomeTypeName.Evaluation
                .MatchesTarget(ctx, this.translatedResultEvaluation, allowSlicing: false) == TypeMatch.Pass;
        }
        internal void EnhanceArguments(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.Arguments)
            {
                IEntityInstance param_eval = this.GetTransParamEvalByArgIndex(arg.Index);
                arg.DataTransfer(ctx, param_eval);
            }
        }

        public IEntityInstance GetTransParamEvalByArgIndex(int argIndex)
        {
            return this.translatedParamEvaluations[this.argParamMapping[argIndex]];
        }
        public FunctionParameter GetParamByArgIndex(int argIndex)
        {
            return this.signature.Parameters[this.argParamMapping[argIndex]];
        }

        private IEnumerable<int> getArgumentIndices(int paramIndex)
        {
            if (paramIndex >= this.paramArgMapping.Count)
                return Enumerable.Empty<int>();
            else
                return this.paramArgMapping[paramIndex];
        }

        private bool isParameterUsed(int paramIndex)
        {
            return this.getArgumentIndices(paramIndex).Any();
        }

        internal IEnumerable<IEnumerable<FunctionArgument>> GetArgumentsMultipleTargeted()
        {
            return this.signature.Parameters
                .Where(param => !param.IsVariadic)
                .Select(param => getArgumentIndices(param.Index))
                .Where(indices => indices.Count() > 1)
                .Select(indices => indices.Select(idx => this.Arguments[idx]));
        }

        internal IEnumerable<FunctionParameter> GetUnfulfilledVariadicParameters()
        {
            return this.signature.Parameters
                .Where(param => param.IsVariadic && !param.Variadic.IsWithinLimits(getArgumentIndices(param.Index).Count()));
        }

        public bool AllParametersUsed()
        {
            bool result = this.signature.Parameters.All(it => isParameterUsed(it.Index));
            return result;
        }

        public bool RequiredParametersUsed()
        {
            bool result = this.signature.Parameters.Where(it => !it.IsOptional).All(it => isParameterUsed(it.Index));
            return result;
        }

        public bool AllArgumentsMapped()
        {
            bool result = this.argParamMapping.All(it => it != noMapping);
            return result;
        }

        private IReadOnlyCollection<INameReference> inferTemplateArguments(ComputationContext ctx)
        {
            if (this.DebugId.Id == 11172)
            {
                ;
            }
            if (!this.directlyTargetsFunction || !this.TargetFunctionInstance.MissingTemplateArguments)
                return null;

            FunctionDefinition function = this.TargetFunctionInstance.TargetTemplate.CastFunction();

            IReadOnlyList<TemplateParameter> template_parameters = this.TargetFunctionInstance.TargetTemplate.Name.Parameters.StoreReadOnlyList();

            var template_param_inference = new Dictionary<TemplateParameter, Tuple<IEntityInstance, bool>>();
            for (int i = 0; i < template_parameters.Count; ++i)
                if (this.templateArguments.Any())
                    template_param_inference.Add(template_parameters[i], Tuple.Create(templateArguments.ElementAt(i).Evaluation, true));
                else
                    template_param_inference.Add(template_parameters[i], Tuple.Create((IEntityInstance)null, false));


            foreach (FunctionArgument arg in this.Arguments)
            {
                IEntityInstance function_param_type = this.GetTransParamEvalByArgIndex(arg.Index);

                IEnumerable<Tuple<TemplateParameter, IEntityInstance>> type_mapping
                    = extractTypeParametersMapping(function, arg.Evaluation, function_param_type);

                foreach (var pair in type_mapping)
                    if (!template_param_inference[pair.Item1].Item2)
                    {
                        if (template_param_inference[pair.Item1].Item1 == null)
                            template_param_inference[pair.Item1] = Tuple.Create(pair.Item2, false);
                        else
                            template_param_inference[pair.Item1] = Tuple.Create(
                                TypeMatcher.LowestCommonAncestor(ctx, template_param_inference[pair.Item1].Item1, pair.Item2),
                                false);
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
                + this.TargetFunctionInstance.ToString() + "(" + this.signature.Parameters.Select(it => it.ToString()).Join(",") + ")";
        }

    }
}
