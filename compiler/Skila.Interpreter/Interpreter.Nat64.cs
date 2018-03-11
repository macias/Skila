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
        private Task<ObjectData> createNat64Async(ExecutionContext ctx, UInt64 value)
        {
            return ObjectData.CreateInstanceAsync(ctx, ctx.Env.Nat64Type.InstanceOf, value);
        }
        private Task<ObjectData> createCharAsync(ExecutionContext ctx, char value)
        {
            return ObjectData.CreateInstanceAsync(ctx, ctx.Env.CharType.InstanceOf, value);
        }

        private async Task<ExecValue> executeNativeNat64FunctionAsync(ExecutionContext ctx, FunctionDefinition func,
            ObjectData thisValue)
        {
            if (func == ctx.Env.Nat64ParseStringFunction)
            {
                ObjectData arg_ptr = ctx.FunctionArguments.Single();
                ObjectData arg_val = arg_ptr.DereferencedOnce();

                string input_str = arg_val.NativeString;
                Option<ObjectData> int_obj;
                if (UInt64.TryParse(input_str, out UInt64 int_val))
                    int_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.Nat64Type.InstanceOf, int_val)
                        .ConfigureAwait(false));
                else
                    int_obj = new Option<ObjectData>();

                ExecValue opt_exec = await createOption(ctx, func.ResultTypeName.Evaluation.Components, int_obj).ConfigureAwait(false);
                if (opt_exec.IsThrow)
                    return opt_exec;
                ObjectData result = opt_exec.ExprValue;
                return ExecValue.CreateReturn(result);
            }
            else if (func.Name.Name == NameFactory.AddOperator)
            {
                var this_int = thisValue.NativeNat64;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat64;

                UInt64 value = checked(this_int + arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.AddOverflowOperator)
            {
                var this_int = thisValue.NativeNat64;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat64;

                UInt64 value1 = this_int + arg_int;
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value1)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.MulOperator)
            {
                var this_int = thisValue.NativeNat64;

                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_int = arg.NativeNat64;

                UInt64 value2 = checked(this_int * arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value2)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.Name.Name == NameFactory.SubOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var this_int = thisValue.NativeNat64;
                var arg_int = arg.NativeNat64;
                UInt64 value = checked(this_int - arg_int);
                ObjectData res_value = await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, value)
                    .ConfigureAwait(false);
                ExecValue result = ExecValue.CreateReturn(res_value);
                return result;
            }
            else if (func.IsDefaultInitConstructor())
            {
                thisValue.Assign(await createNat64Async(ctx, 0UL).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else if (func.IsCopyInitConstructor())
            {
                thisValue.Assign(ctx.FunctionArguments.Single());
                return ExecValue.CreateReturn(null);
            }
            else if (func == ctx.Env.Nat64FromNat8Constructor)
            {
                ObjectData arg_obj = ctx.FunctionArguments.Single();
                var arg_val = arg_obj.NativeNat8;

                thisValue.Assign(await ObjectData.CreateInstanceAsync(ctx, thisValue.RunTimeTypeInstance, (UInt64)arg_val).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else if (func.Name.Name == NameFactory.ComparableCompare)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var this_int = thisValue.NativeNat64;
                var arg_int = arg.NativeNat64;

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
                ExecValue? result = await numComparisonAsync<UInt64>(ctx, func, thisValue).ConfigureAwait(false);
                if (result.HasValue)
                    return result.Value;
                else
                    throw new NotImplementedException($"Function {func} is not implemented");
            }
        }
    }
}
