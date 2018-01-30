using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;
using System.Threading.Tasks;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        private async Task<ExecValue> executeNativeIntFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData thisValue)
        {
            if (func == ctx.Env.IntParseStringFunction)
            {
                ObjectData arg_ptr = ctx.FunctionArguments.Single();
                ObjectData arg_val = arg_ptr.DereferencedOnce();

                string input_str = arg_val.PlainValue.Cast<string>();
                Option<ObjectData> int_obj;
                if (int.TryParse(input_str, out int int_val))
                    int_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.IntType.InstanceOf, int_val)
                        .ConfigureAwait(false));
                else
                    int_obj = new Option<ObjectData>();

                ObjectData result = await createOption(ctx, func.ResultTypeName.Evaluation.Components, int_obj).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func.Name.Name == NameFactory.AddOperator)
            {
                int this_int = thisValue.PlainValue.Cast<int>();

                ObjectData arg = ctx.FunctionArguments.Single();
                int arg_int = arg.PlainValue.Cast<int>();

                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, this_int + arg_int)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.MulOperator)
            {
                int this_int = thisValue.PlainValue.Cast<int>();

                ObjectData arg = ctx.FunctionArguments.Single();
                int arg_int = arg.PlainValue.Cast<int>();

                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, this_int * arg_int)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.SubOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                int this_int = thisValue.PlainValue.Cast<int>();
                int arg_int = arg.PlainValue.Cast<int>();
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, this_int - arg_int)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.EqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                int this_int = thisValue.PlainValue.Cast<int>();
                int arg_int = arg.PlainValue.Cast<int>();
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    this_int == arg_int).ConfigureAwait(false));
                return result;
            }
            else if (func.IsDefaultInitConstructor())
            {
                thisValue.Assign(await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, 0).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else if (func.IsCopyInitConstructor())
            {
                thisValue.Assign(ctx.FunctionArguments.Single());
                return ExecValue.CreateReturn(null);
            }
            else if (func.Name.Name == NameFactory.ComparableCompare)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                int this_int = thisValue.PlainValue.Cast<int>();
                int arg_int = arg.PlainValue.Cast<int>();

                ObjectData ordering_type = await ctx.TypeRegistry.RegisterGetAsync(ctx, ctx.Env.OrderingType.InstanceOf).ConfigureAwait(false);
                ObjectData ordering_value;
                if (this_int < arg_int)
                    ordering_value = ordering_type.GetField(ctx.Env.OrderingLess);
                else if (this_int > arg_int)
                    ordering_value = ordering_type.GetField(ctx.Env.OrderingGreater);
                else
                    ordering_value = ordering_type.GetField(ctx.Env.OrderingEqual);

                ExecValue result = ExecValue.CreateReturn(ordering_value);
                return result;
            }
            else
                throw new NotImplementedException($"Function {func} is not implemented");
        }
    }
}
