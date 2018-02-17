using System;
using System.Linq;
using Skila.Language.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        private static async Task<ExecValue> executeNativeRegexFunctionAsync(ExecutionContext ctx, FunctionDefinition func, 
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

                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");

                var elements = new List<ObjectData>();
                for (int i=0;i<matches.Count;++i)
                {
                    System.Text.RegularExpressions.Match match = matches[i];
                    

                   // elements.Add();
                }
                // todo: continue work here!!!

                ExecValue result = ExecValue.CreateReturn(await createChunkOnHeap(ctx,ctx.Env.MatchType.InstanceOf,elements).ConfigureAwait(false));
                return result;
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

    }
}
