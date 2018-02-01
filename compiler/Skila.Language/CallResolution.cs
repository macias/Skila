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
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance)
        {
            // at this point we target true function (any function-like object is converted to closure already)
            if (!targetFunctionInstance.Target.IsFunction())
                throw new Exception("Internal error");

            List<int> arg_param_mapping = createArgParamMapping(targetFunctionInstance, argumentsProvider.Arguments);
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

        public IReadOnlyList<FunctionArgument> Arguments => this.argumentsProvider.Arguments;
        public IReadOnlyCollection<INameReference> InferredTemplateArguments { get; }
        private readonly IReadOnlyList<IEntityInstance> templateArguments;

        public FunctionArgument MetaThisArgument { get; private set; } // null for regular functions (not-methods)
        public EvaluationInfo Evaluation => this.translatedResultEvaluation;

        private CallResolution(ComputationContext ctx,
            IEnumerable<INameReference> templateArguments,
            IFunctionArgumentsProvider argumentsProvider,
            CallContext callContext,
            EntityInstance targetFunctionInstance,
            List<int> argParamMapping,
            out bool success)
        {
            if (argumentsProvider.DebugId.Id == 27281 && targetFunctionInstance.DebugId.Id == 128265)
            {
                ;
            }

            success = true;

            this.MetaThisArgument = callContext.MetaThisArgument;
            this.TargetFunctionInstance = targetFunctionInstance;
            this.argumentsProvider = argumentsProvider;
            this.typeMatches = new TypeMatch?[this.TargetFunction.Parameters.Count];
            this.templateArguments = (templateArguments.Any()
                ? templateArguments.Select(it => it.Evaluation.Components)
                : this.TargetFunction.Name.Parameters.Select(it => EntityInstance.Joker)).StoreReadOnlyList();
            this.argParamMapping = argParamMapping;
            this.paramArgMapping = createParamArgMapping(this.argParamMapping);

            if (this.DebugId.Id == 670)
            {
                ;
            }

            IEntityInstance call_ctx_eval = callContext.Evaluation;
            if (call_ctx_eval != null)
                // we need to have evaluation of the value, not ref/ptr, so the correct template translation table could kick in
                ctx.Env.Dereference(call_ctx_eval, out call_ctx_eval); 

            extractParameters(ctx, call_ctx_eval, this.TargetFunctionInstance,
                out this.translatedParamEvaluations,
                out this.translatedResultEvaluation);

            if (this.templateArguments.Any(it => it.IsJoker))
            {
                IEnumerable<IEntityInstance> inferred = inferTemplateArgumentsFromExpressions(ctx);

                inferred = inferTemplateArgumentsFromConstraints(ctx, inferred);

                if (inferred.Any(it => it.IsJoker))
                    success = false;
                else
                {
                    this.InferredTemplateArguments = inferred.Select(it => it.NameOf).StoreReadOnly();

                    this.TargetFunctionInstance = EntityInstance.Create(this.TargetFunctionInstance, inferred,
                        this.TargetFunctionInstance.OverrideMutability);

                    extractParameters(ctx, call_ctx_eval, this.TargetFunctionInstance,
                        out this.translatedParamEvaluations,
                        out this.translatedResultEvaluation);

                }
            }
        }

        private static List<int> createArgParamMapping(EntityInstance targetFunctionInstance,
            IReadOnlyList<FunctionArgument> arguments)
        {
            FunctionDefinition target_function = targetFunctionInstance.Target.CastFunction();

            List<int> argParamMapping = Enumerable.Repeat(noMapping, arguments.Count).ToList();

            {
                int param_idx = 0;
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
            foreach (var arg in this.Arguments)
                arg.SetTargetParam(ctx, this.GetParamByArgIndex(arg.Index));
        }

        private static void extractParameters(ComputationContext ctx,
            IEntityInstance objectInstance,
            EntityInstance targetFunctionInstance,
            out IReadOnlyList<ParameterType> translatedParamEvaluations,
            out EvaluationInfo translatedResultEvaluation)
        {
            FunctionDefinition target_function = targetFunctionInstance.TargetTemplate.CastFunction();

            translatedParamEvaluations = target_function.Parameters
                .Select(it => ParameterType.Create(it, objectInstance, targetFunctionInstance))
                .StoreReadOnlyList();

            target_function.ResultTypeName.Evaluated(ctx);

            IEntityInstance components = orderedTranslatation(target_function.ResultTypeName.Evaluation.Components,
                objectInstance, targetFunctionInstance);

            EntityInstance aggregate = orderedTranslatation(target_function.ResultTypeName.Evaluation.Aggregate,
                objectInstance, targetFunctionInstance);

            translatedResultEvaluation = new EvaluationInfo(components, aggregate);
        }

        private static T orderedTranslatation<T>(T instance, IEntityInstance objectInstance,
            EntityInstance targetFunctionInstance)
            where T : IEntityInstance
        {
            instance = instance.TranslateThrough(targetFunctionInstance);
            instance = instance.TranslateThrough(objectInstance);
            return instance;
        }

        internal bool ArgumentTypesMatchParameters(ComputationContext ctx)
        {
            foreach (FunctionArgument arg in this.Arguments)
            {
                if (arg.DebugId.Id == 326)
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

        public IEnumerable<FunctionArgument> GetArguments(int paramIndex)
        {
            if (paramIndex >= this.paramArgMapping.Count)
                return Enumerable.Empty<FunctionArgument>();
            else
                return this.paramArgMapping[paramIndex].Select(arg_idx => this.Arguments[arg_idx]);
        }

        private bool isParameterUsed(int paramIndex)
        {
            return this.GetArguments(paramIndex).Any();
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
            bool result = this.TargetFunction.Parameters.Where(it => !it.IsOptional).All(it => isParameterUsed(it.Index));
            return result;
        }

        private IEnumerable<IEntityInstance> inferTemplateArgumentsFromExpressions(ComputationContext ctx)
        {
            if (this.DebugId.Id == 130076)
            {
                ;
            }

            IReadOnlyList<TemplateParameter> template_parameters = this.TargetFunctionInstance.TargetTemplate.Name.Parameters;

            var template_param_inference = new Dictionary<TemplateParameter,
                // true -- set by user, don't change
                Tuple<IEntityInstance, bool>>();
            for (int i = 0; i < template_parameters.Count; ++i)
                if (this.templateArguments[i].IsJoker)
                    template_param_inference.Add(template_parameters[i], Tuple.Create((IEntityInstance)null, false));
                else
                    template_param_inference.Add(template_parameters[i], Tuple.Create(templateArguments[i], true));


            foreach (FunctionArgument arg in this.Arguments)
            {
                IEntityInstance function_param_type = this.GetTransParamEvalByArg(arg);

                IEnumerable<Tuple<TemplateParameter, IEntityInstance>> type_mapping
                    = extractTypeParametersMapping(ctx, this.TargetFunction, arg.Evaluation.Components, function_param_type);

                foreach (Tuple<TemplateParameter, IEntityInstance> pair in type_mapping)
                {
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
            }

            return template_parameters.Select(it => template_param_inference[it].Item1 ?? EntityInstance.Joker);
        }

        private IEnumerable<IEntityInstance> inferTemplateArgumentsFromConstraints(ComputationContext ctx,
            IEnumerable<IEntityInstance> templateArguments)
        {
            if (templateArguments.All(it => !it.IsJoker))
                return templateArguments;

            if (this.DebugId.Id == 71160)
            {
                ;
            }

            EntityInstance closedTemplate = EntityInstance.Create(this.TargetFunctionInstance, templateArguments,
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

        private static IEnumerable<Tuple<TemplateParameter, IEntityInstance>> extractTypeParametersMapping(ComputationContext ctx,
            TemplateDefinition template,
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
                return Enumerable.Empty<Tuple<TemplateParameter, IEntityInstance>>();

            // case when we have direct hit, function: def foo<T>(x T)
            // and we have argument (for example) Int against parameter "x T", thus we simply match them: T=Int
            if (param_type_instance.IsTemplateParameterOf(template))
                return new[] { Tuple.Create(param_type_instance.TargetType.TemplateParameter, argType) };
            else
            {
                var arg_type_instance = argType as EntityInstance;
                if (arg_type_instance == null)
                    return Enumerable.Empty<Tuple<TemplateParameter, IEntityInstance>>();

                IEnumerable<EntityInstance> arg_family = arg_type_instance.Inheritance(ctx).AncestorsWithoutObject
                    .Concat(arg_type_instance);
                arg_type_instance = arg_family
                    .SingleOrDefault(it => it.TargetType == param_type_instance.TargetType);

                if (arg_type_instance == null)
                    return Enumerable.Empty<Tuple<TemplateParameter, IEntityInstance>>();

                var zipped = arg_type_instance.TemplateArguments.SyncZip(param_type_instance.TemplateArguments);
                return zipped
                    .Select(it => extractTypeParametersMapping(ctx, template, it.Item1, it.Item2))
                    .Flatten()
                    .ToArray();
            }
        }

        public override string ToString()
        {
            return "(" + this.Arguments.Select(it => $"{it}").Join(",") + ") -> "
                + $"{this.TargetFunctionInstance}(" + this.TargetFunction.Parameters.Select(it => $"{it}").Join(",") + ")";
        }

    }
}
