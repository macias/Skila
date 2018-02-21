using System;
using System.Linq;
using Skila.Language.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Skila.Language;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
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

                System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(pattern);
                System.Text.RegularExpressions.MatchCollection matches = regex.Matches(arg_str);

                var elements = new List<ObjectData>();
                for (int match_idx = 0; match_idx < matches.Count; ++match_idx)
                {
                    System.Text.RegularExpressions.Match match = matches[match_idx];
                    ObjectData match_index_val = await createNat64Async(ctx, (UInt64)match.Index).ConfigureAwait(false);
                    ObjectData match_length_val = await createNat64Async(ctx, (UInt64)match.Length).ConfigureAwait(false);

                    ObjectData array_captures_ptr;

                    {
                        if (!ctx.Env.DereferencedOnce(ctx.Env.MatchCapturesProperty.TypeName.Evaluation.Components,
                            out IEntityInstance array_captures_type, out bool dummy))
                            throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

                        ExecValue ret = await createObject(ctx, true, array_captures_type, ctx.Env.ArrayDefaultConstructor, null)
                            .ConfigureAwait(false);

                        if (ret.Mode == DataMode.Throw)
                            return ret;

                        array_captures_ptr = ret.ExprValue;
                        ctx.Heap.TryInc(ctx, array_captures_ptr, RefCountIncReason.StoringLocalPointer, "");

                        // skipping implicit "everything" group
                        for (int grp_idx = 1; grp_idx < match.Groups.Count; ++grp_idx)
                        {
                            System.Text.RegularExpressions.Group group = match.Groups[grp_idx];
                            string group_name = regex.GroupNameFromNumber(grp_idx);
                            if (group_name == $"{grp_idx}") // hack for anonymous captures
                                group_name = null;

                            for (int cap_idx = 0; cap_idx < group.Captures.Count; ++cap_idx)
                            {
                                System.Text.RegularExpressions.Capture cap = group.Captures[cap_idx];

                                ObjectData cap_index_val = await createNat64Async(ctx, (UInt64)cap.Index).ConfigureAwait(false);
                                ObjectData cap_length_val = await createNat64Async(ctx, (UInt64)cap.Length).ConfigureAwait(false);
                                ObjectData cap_opt_name_val;
                                {
                                    Option<ObjectData> opt_group_name_obj;
                                    if (group_name != null)
                                    {
                                        ObjectData str_ptr = await createStringAsync(ctx, group_name).ConfigureAwait(false);
                                        opt_group_name_obj = new Option<ObjectData>(str_ptr);
                                    }
                                    else
                                        opt_group_name_obj = new Option<ObjectData>();

                                    IEntityInstance opt_cap_type = ctx.Env.CaptureConstructor.Parameters.Last().TypeName.Evaluation.Components;
                                    ExecValue opt_exec = await createOption(ctx, opt_cap_type, opt_group_name_obj).ConfigureAwait(false);
                                    if (opt_exec.IsThrow)
                                        return opt_exec;
                                    cap_opt_name_val = opt_exec.ExprValue;
                                }
                                ExecValue capture_obj_exec = await createObject(ctx, false, ctx.Env.CaptureType.InstanceOf,
                                    ctx.Env.CaptureConstructor, null, cap_index_val, cap_length_val, cap_opt_name_val).ConfigureAwait(false);
                                if (capture_obj_exec.Mode == DataMode.Throw)
                                    return capture_obj_exec;
                                ObjectData capture_ref = await capture_obj_exec.ExprValue.ReferenceAsync(ctx).ConfigureAwait(false);

                                ExecValue append_exec = await callNonVariadicFunctionDirectly(ctx, ctx.Env.ArrayAppendFunction, null,
                                    array_captures_ptr, capture_ref).ConfigureAwait(false);
                                if (append_exec.Mode == DataMode.Throw)
                                    return append_exec;
                            }
                        }

                    }
                    ObjectData match_val;
                    {
                        ExecValue ret = await createObject(ctx, false, ctx.Env.MatchType.InstanceOf,
                                ctx.Env.MatchConstructor, null, match_index_val, match_length_val, array_captures_ptr).ConfigureAwait(false);
                        ctx.Heap.TryRelease(ctx, array_captures_ptr, null, false, RefCountDecReason.DroppingLocalPointer, "");

                        if (ret.Mode == DataMode.Throw)
                            return ret;

                        match_val = ret.ExprValue;
                    }

                    elements.Add(match_val);
                }

                ObjectData heap_chunk = await createChunkOnHeap(ctx, ctx.Env.MatchType.InstanceOf, elements).ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(heap_chunk);
                return result;
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

    }
}
