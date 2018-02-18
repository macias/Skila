using System;
using System.Linq;
using Skila.Language.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Skila.Language;
using Skila.Language.Extensions;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        private async Task<ExecValue> callNonVariadicFunctionDirectly(ExecutionContext ctx,FunctionDefinition targetFunc, 
            IEnumerable<IEntityInstance> templateArguments,
            ObjectData thisValue,params ObjectData[] arguments)
        {
            // btw. arguments have to be given in exact order as parameters go

            if (targetFunc.Parameters.Any(it => it.IsVariadic))
                throw new Exception($"{ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, thisValue, $"{targetFunc}").ConfigureAwait(false);
            ObjectData[] args = await prepareArguments(ctx, targetFunc, 
                // that is why this function does not handle variadic, it assumes single argument per parameter
                arguments.Select(it =>  new[] { it }).ToArray()
                ).ConfigureAwait(false);
            SetupFunctionCallData(ref ctx, templateArguments, this_ref, args);
            ExecValue ret = await ExecutedAsync(targetFunc, ctx).ConfigureAwait(false);
            return ret;
        }
        private async Task<ExecValue> executeNativeRegexFunctionAsync(ExecutionContext ctx, FunctionDefinition func,
            ObjectData thisValue)
        {
            if (func == ctx.Env.RegexContainsFunction)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                ObjectData arg_val = arg.DereferencedOnce();
                string arg_str = arg_val.NativeString;

                ObjectData pattern_obj = thisValue.GetField(ctx.Env.RegexPatternField);
                ObjectData pattern_val = pattern_obj.DereferencedOnce();
                string pattern = pattern_val.NativeString;

                bool val = new System.Text.RegularExpressions.Regex(pattern).IsMatch(arg_str);

                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components, val).ConfigureAwait(false));
                return result;
            }
            else if (func == ctx.Env.RegexMatchFunction)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                ObjectData arg_val = arg.DereferencedOnce();
                string arg_str = arg_val.NativeString;

                ObjectData pattern_obj = thisValue.GetField(ctx.Env.RegexPatternField);
                ObjectData pattern_val = pattern_obj.DereferencedOnce();
                string pattern = pattern_val.NativeString;

                System.Text.RegularExpressions.MatchCollection matches = new System.Text.RegularExpressions.Regex(pattern)
                    .Matches(arg_str);

                var elements = new List<ObjectData>();
                for (int i = 0; i < matches.Count; ++i)
                {
                    System.Text.RegularExpressions.Match match = matches[i];
                    ObjectData index_val = await createNat64Async(ctx, (UInt64)match.Index).ConfigureAwait(false);
                    ObjectData length_val = await createNat64Async(ctx, (UInt64)match.Length).ConfigureAwait(false);

                    ObjectData array_captures_ptr;

                    {
                        if (!ctx.Env.DereferencedOnce(ctx.Env.MatchCapturesProperty.TypeName.Evaluation.Components,
                            out IEntityInstance array_captures_type, out bool dummy))
                            throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");
                        IEntityInstance array_elem_type = array_captures_type.Cast<EntityInstance>().TemplateArguments.Single();
                        array_captures_ptr = await allocObjectAsync(ctx, array_captures_type,
                            ctx.Env.MatchCapturesProperty.TypeName.Evaluation.Components, null).ConfigureAwait(false);

                        // it is local variable so we need to inc ref count
                        ctx.Heap.TryInc(ctx, array_captures_ptr, RefCountIncReason.BuildingRegexCaptures, "");

                        ExecValue ret = await callNonVariadicFunctionDirectly(ctx, ctx.Env.ArrayDefaultConstructor, null, 
                            array_captures_ptr).ConfigureAwait(false);

                        if (ret.Mode != DataMode.Return)
                            return ret;
                    }
                    ObjectData match_val;
                    {
                        match_val = await allocObjectAsync(ctx, ctx.Env.MatchType.InstanceOf,
                            ctx.Env.MatchType.InstanceOf, null).ConfigureAwait(false);

                        ExecValue ret = await callNonVariadicFunctionDirectly(ctx,ctx.Env.MatchConstructor, null, match_val,
                             index_val, length_val ,  array_captures_ptr).ConfigureAwait(false);

                        if (ret.Mode != DataMode.Return)
                            return ret;
                    }

                    // cleaning current scope, so we need to get rid of this local "variable"
                    ctx.Heap.TryRelease(ctx, array_captures_ptr, null, false, RefCountDecReason.BuildingRegexCaptures, "");

                    elements.Add(match_val);
                }

                ExecValue result = ExecValue.CreateReturn(await createChunkOnHeap(ctx, ctx.Env.MatchType.InstanceOf, elements).ConfigureAwait(false));
                return result;
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

    }
}
