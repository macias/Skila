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
        private async Task<ExecValue> executeNativeNat8FunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData thisValue)
        {
            if (func == ctx.Env.Nat8ParseStringFunction)
            {
                ObjectData arg_ptr = ctx.FunctionArguments.Single();
                ObjectData arg_val = arg_ptr.DereferencedOnce();

                string input_str = arg_val.NativeString;
                Option<ObjectData> int_obj;
                if (byte.TryParse(input_str, out byte int_val))
                    int_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.Nat8Type.InstanceOf, int_val)
                        .ConfigureAwait(false));
                else
                    int_obj = new Option<ObjectData>();

                ObjectData result = await createOption(ctx, func.ResultTypeName.Evaluation.Components, int_obj).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func.Name.Name == NameFactory.AddOperator)
            {
                var this_int = thisValue.NativeNat8;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat8;

                byte value = (byte)checked(this_int + arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.AddOverflowOperator)
            {
                var this_int = thisValue.NativeNat8;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat8;

                byte value = (byte)(this_int + arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.MulOperator)
            {
                var this_int = thisValue.NativeNat8;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat8;

                byte value = (byte)checked(this_int * arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.SubOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var this_int = thisValue.NativeNat8;
                var arg_int = arg.NativeNat8;
                byte value = (byte)checked(this_int - arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.IsDefaultInitConstructor())
            {
                thisValue.Assign(await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, (byte)0).ConfigureAwait(false));
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
                var this_int = thisValue.NativeNat8;
                var arg_int = arg.NativeNat8;

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
            {
                ExecValue? result = await numComparisonAsync<byte>(ctx, func, thisValue).ConfigureAwait(false);
                if (result.HasValue)
                    return result.Value;
                else
                    throw new NotImplementedException($"Function {func} is not implemented");
            }
        }
    }
}
