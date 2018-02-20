﻿using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        private async Task<ExecValue?> numComparisonAsync<T>(ExecutionContext ctx, FunctionDefinition func, ObjectData thisValue)
            where T : IComparable
        {
            var this_num = thisValue.PlainValue.Cast<T>();

            if (func.Name.Name == NameFactory.EqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_num.Equals(arg_num);
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.NotEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = !this_num.Equals(arg_num);
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.LessOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_num.CompareTo(arg_num) < 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.LessEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_num.CompareTo(arg_num) <= 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.GreaterOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_num.CompareTo(arg_num) > 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.GreaterEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_num.CompareTo(arg_num) >= 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }

            return null;
        }

        private async Task<ExecValue> createObject(ExecutionContext ctx,
            bool onHeap, // false -> create regular value
            IEntityInstance typename,
            FunctionDefinition targetFunc,
            IEnumerable<IEntityInstance> templateArguments,
            params ObjectData[] arguments)
        {
            IEntityInstance outer_typename = typename;
            if (onHeap)
                outer_typename = ctx.Env.Reference(typename, MutabilityFlag.ConstAsSource, null, viaPointer: true);
            ObjectData this_object = await allocObjectAsync(ctx, typename, outer_typename, null).ConfigureAwait(false);

            // it is local variable so we need to inc ref count
            bool incremented = ctx.Heap.TryInc(ctx, this_object, RefCountIncReason.StoringLocalPointer, "");

            ExecValue ret = await callNonVariadicFunctionDirectly(ctx, targetFunc, templateArguments,
                this_object, arguments).ConfigureAwait(false);

            if (incremented)
                ctx.Heap.TryRelease(ctx, this_object, this_object, false, RefCountDecReason.DroppingLocalPointer, "");

            if (ret.Mode == DataMode.Throw)
                return ret;
            else
                return ExecValue.CreateExpression(this_object);
        }

        private async Task<ExecValue> callNonVariadicFunctionDirectly(ExecutionContext ctx, FunctionDefinition targetFunc,
            IEnumerable<IEntityInstance> templateArguments,
            ObjectData thisValue, params ObjectData[] arguments)
        {
            // btw. arguments have to be given in exact order as parameters go

            if (targetFunc.Parameters.Any(it => it.IsVariadic))
                throw new Exception($"{ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, thisValue, $"{targetFunc}").ConfigureAwait(false);
            ObjectData[] args = await prepareArguments(ctx, targetFunc,
                // that is why this function does not handle variadic, it assumes single argument per parameter
                arguments.Select(it => new[] { it }).ToArray()
                ).ConfigureAwait(false);
            SetupFunctionCallData(ref ctx, templateArguments, this_ref, args);
            ExecValue ret = await ExecutedAsync(targetFunc, ctx).ConfigureAwait(false);
            return ret;
        }

        private async Task<ExecValue> executeNativeFunctionAsync(ExecutionContext ctx, FunctionDefinition func)
        {
            TypeDefinition owner_type = func.ContainingType();

            // meta-this is always passed as reference or pointer, so we can blindly dereference it
            ObjectData this_value = func.Modifier.HasStatic ? null : ctx.ThisArgument.DereferencedOnce();

            if (owner_type.Modifier.HasEnum)
                return await executeNativeEnumFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.UnitType)
                return await executeNativeUnitFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.StringType)
                return await executeNativeStringFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.RegexType)
                return await executeNativeRegexFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.FileType)
                return await executeNativeFileFunctionAsync(ctx, func).ConfigureAwait(false);
            else if (owner_type == ctx.Env.ChunkType)
                return await executeNativeChunkFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.Int16Type)
                return await executeNativeInt16FunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.Int64Type)
                return await executeNativeInt64FunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.Nat8Type)
                return await executeNativeNat8FunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.Nat64Type)
                return await executeNativeNat64FunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.Real64Type)
                return await executeNativeReal64FunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.IObjectType)
                return await executeNativeIObjectFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.BoolType)
                return await executeNativeBoolFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.DateType)
                return await executeNativeDateFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else if (owner_type == ctx.Env.ChannelType)
                return await executeNativeChannelFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            else
            {
                throw new NotImplementedException($"{owner_type}");
            }
        }

        private static async Task<ExecValue> executeNativeEnumFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func.IsInitConstructor()
                && func.Parameters.Count == 1 && func.Parameters.Single().Name.Name == NameFactory.EnumConstructorParameter)
            {
                ObjectData arg_obj = ctx.FunctionArguments.Single();
                // changing runtime type from some kind enum (Nat/Int) to truly enum
                ObjectData enum_obj = await  ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, arg_obj.PlainValue).ConfigureAwait(false);
                this_value.Assign(enum_obj);
                return ExecValue.CreateReturn(null);
            }
            else if (func.Name.Name == NameFactory.EqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var this_int = this_value.NativeNat;
                var arg_int = arg.NativeNat;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, 
                    func.ResultTypeName.Evaluation.Components, this_int == arg_int).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.NotEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var this_int = this_value.NativeNat;
                var arg_int = arg.NativeNat;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components, this_int != arg_int).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.ConvertFunctionName)
            {
                if (ctx.Env.IsNatType(func.ResultTypeName.Evaluation.Components))
                {
                    ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                        this_value.PlainValue).ConfigureAwait(false);
                    return ExecValue.CreateReturn(result);
                }
                else
                    throw new NotImplementedException($"Enum func {func} is not implemented");
            }
            else
                throw new NotImplementedException($"Function {func} is not implemented");
        }

        private static async Task<ExecValue> executeNativeUnitFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func.IsDefaultInitConstructor())
            {
                this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, UnitType.UnitValue).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

        private static async Task<ExecValue> executeNativeStringFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func == ctx.Env.StringCountGetter)
            {
                string native_object = this_value.NativeString;
                ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    (UInt64)native_object.Length).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

        private async Task<ExecValue> executeNativeFileFunctionAsync(ExecutionContext ctx, FunctionDefinition func)
        {
            if (func == ctx.Env.FileReadLines)
            {
                ObjectData filepath_obj = ctx.GetArgument(func, NameFactory.FileFilePathParameter);
                if (!filepath_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData filepath_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                string filepath = filepath_val.PlainValue.Cast<string>();

                string[] lines = null;
                try
                {
                    // todo: change it to ReadLines once we have deferred execution
                    lines = System.IO.File.ReadAllLines(filepath);
                }
#pragma warning disable 0168
                catch (Exception ex) // we would use it when debugging
#pragma warning restore 0168
                {
                }

                Option<ObjectData> lines_obj;

                if (lines == null)
                    lines_obj = new Option<ObjectData>();
                else
                {
                    IEntityInstance string_ptr_instance;
                    {
                        IEntityInstance ptr_iterable_instance = func.ResultTypeName.Evaluation.Components.Cast<EntityInstance>().TemplateArguments.Single();
                        IEntityInstance iterable_str_ptr_instance = ptr_iterable_instance.Cast<EntityInstance>().TemplateArguments.Single();
                        string_ptr_instance = iterable_str_ptr_instance.Cast<EntityInstance>().TemplateArguments.Single();
                    }

                    var chunk = new ObjectData[lines.Length];
                    int i = 0;
                    foreach (string s in lines)
                    {
                        ObjectData s_ptr = await allocObjectAsync(ctx, ctx.Env.StringType.InstanceOf, string_ptr_instance, s).ConfigureAwait(false);
                        if (!ctx.Heap.TryInc(ctx, s_ptr, RefCountIncReason.FileLine, $"{filepath}"))
                            throw new Exception($"{ExceptionCode.SourceInfo()}");

                        chunk[i] = s_ptr;
                        ++i;
                    }

                    ObjectData chunk_ptr = await createChunkOnHeap(ctx, string_ptr_instance, chunk).ConfigureAwait(false);

                    lines_obj = new Option<ObjectData>(chunk_ptr);
                }


                ObjectData result = await createOption(ctx, func.ResultTypeName.Evaluation.Components, lines_obj).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func == ctx.Env.FileExists)
            {
                ObjectData filepath_obj = ctx.GetArgument(func, NameFactory.FileFilePathParameter);
                if (!filepath_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData filepath_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                string filepath = filepath_val.PlainValue.Cast<string>();

                bool exists = System.IO.File.Exists(filepath);

                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    exists).ConfigureAwait(false));
                return result;
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

        private async Task<ExecValue> executeNativeChannelFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func.IsDefaultInitConstructor())
            {
                Channels.IChannel<ObjectData> channel = Channels.Channel.Create<ObjectData>();
                ctx.Heap.TryAddDisposable(channel);
                ObjectData channel_obj = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, channel).ConfigureAwait(false);
                this_value.Assign(channel_obj);
                return ExecValue.CreateReturn(null);
            }
            else if (func.Name.Name == NameFactory.ChannelSend)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                if (!ctx.Env.IsPointerOfType(arg.RunTimeTypeInstance))
                    arg = arg.Copy();

                Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                bool result = await channel.SendAsync(arg).ConfigureAwait(false);
                return ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, result).ConfigureAwait(false));
            }
            else if (func.Name.Name == NameFactory.ChannelReceive)
            {
                Channels.IChannel<ObjectData> channel = this_value.PlainValue.Cast<Channels.IChannel<ObjectData>>();
                EntityInstance channel_type = this_value.RunTimeTypeInstance;
                IEntityInstance value_type = channel_type.TemplateArguments.Single();

                Option<ObjectData> received = await channel.ReceiveAsync().ConfigureAwait(false);

                // we have to compute Skila Option type (not C# one we use for C# channel type)
                EntityInstance option_type = ctx.Env.OptionType.GetInstance(new[] { value_type }, overrideMutability: MutabilityFlag.ConstAsSource,
                    translation: null);
                ObjectData result = await createOption(ctx, option_type, received).ConfigureAwait(false);

                // at this point Skila option is initialized so we can return it

                return ExecValue.CreateReturn(result);
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

        private static async Task<ExecValue> executeNativeDateFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func == ctx.Env.DateDayOfWeekGetter)
            {
                // todo: change the types of the Date in Skila to Int32, Nat8, Nat8
                ObjectData year_obj = this_value.GetField(ctx.Env.DateYearField);
                var year = year_obj.NativeInt16;
                ObjectData month_obj = this_value.GetField(ctx.Env.DateMonthField);
                var month = month_obj.NativeNat8;
                ObjectData day_obj = this_value.GetField(ctx.Env.DateDayField);
                var day = day_obj.NativeNat8;

                UInt64 day_of_week = (UInt64)(new DateTime(year, month, day).DayOfWeek);

                ObjectData ret_obj = await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components, day_of_week).ConfigureAwait(false);

                return ExecValue.CreateReturn(ret_obj);
            }
            else
            {
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
            }
        }

        private static async Task<ExecValue> executeNativeBoolFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func.IsDefaultInitConstructor())
            {
                this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, false).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else if (func.IsCopyInitConstructor())
            {
                this_value.Assign(ctx.FunctionArguments.Single());
                return ExecValue.CreateReturn(null);
            }
            else if (func.Name.Name == NameFactory.NotOperator)
            {
                return ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance,
                    !this_value.PlainValue.Cast<bool>()).ConfigureAwait(false));
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

        private static async Task<ExecValue> executeNativeIObjectFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func == ctx.Env.IObjectGetTypeFunction)
            {
                // todo: add some real TypeInfo object, for now it is empty so we can return whatever is unique
                ObjectData fake = await ctx.TypeRegistry.RegisterGetAsync(ctx, this_value.RunTimeTypeInstance).ConfigureAwait(false);
                fake = await fake.ReferenceAsync(ctx).ConfigureAwait(false);
                return ExecValue.CreateReturn(fake);
            }
            else
                throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
        }

    }
}
