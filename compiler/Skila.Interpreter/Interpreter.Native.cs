using System;
using System.Linq;
using Skila.Language;
using Skila.Language.Entities;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Skila.Interpreter
{
    public sealed partial class Interpreter : IInterpreter
    {
        private static async Task<ExecValue?> equalityTestAsync<T>(ExecutionContext ctx, FunctionDefinition func, ObjectData thisValue,
            bool heapArguments)
            where T : IComparable
        {
            var this_native = thisValue.PlainValue.Cast<T>();

            if (func.Name.Name == NameFactory.EqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                if (heapArguments)
                    arg = arg.DereferencedOnce();
                var arg_native = arg.PlainValue.Cast<T>();
                bool cmp = this_native.Equals(arg_native);
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.NotEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                if (heapArguments)
                    arg = arg.DereferencedOnce();
                var arg_native = arg.PlainValue.Cast<T>();
                bool cmp = !this_native.Equals(arg_native);
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }

            return null;
        }

        private async Task<ExecValue?> numComparisonAsync<T>(ExecutionContext ctx, FunctionDefinition func, ObjectData thisValue)
            where T : IComparable
        {
            var eq_result = await equalityTestAsync<T>(ctx, func, thisValue, heapArguments: false).ConfigureAwait(false);
            if (eq_result.HasValue)
                return eq_result;

            var this_native = thisValue.PlainValue.Cast<T>();

            if (func.Name.Name == NameFactory.LessOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_native.CompareTo(arg_num) < 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.LessEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_native.CompareTo(arg_num) <= 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.GreaterOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_native.CompareTo(arg_num) > 0;
                ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx,
                    func.ResultTypeName.Evaluation.Components,
                    cmp).ConfigureAwait(false));
                return result;
            }
            else if (func.Name.Name == NameFactory.GreaterEqualOperator)
            {
                ObjectData arg = ctx.FunctionArguments.Single();
                var arg_num = arg.PlainValue.Cast<T>();
                bool cmp = this_native.CompareTo(arg_num) >= 0;
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
                outer_typename = ctx.Env.Reference(typename, MutabilityOverride.None, null, viaPointer: true);
            ObjectData this_object = await allocObjectAsync(ctx, typename, outer_typename, null).ConfigureAwait(false);

            // it is local variable so we need to inc ref count
            bool incremented = ctx.Heap.TryInc(ctx, this_object, RefCountIncReason.StoringLocalPointer, "");

            ExecValue ret = await callNonVariadicFunctionDirectly(ctx, targetFunc, templateArguments,
                this_object, arguments).ConfigureAwait(false);

            if (incremented)
                ctx.Heap.TryRelease(ctx, this_object, this_object, false, RefCountDecReason.DroppingLocalPointer, "");

            if (ret.IsThrow)
                return ret;
            else
                return ExecValue.CreateExpression(this_object);
        }

        private async Task<ExecValue> callNonVariadicFunctionDirectly(ExecutionContext ctx, FunctionDefinition targetFunc,
            IEnumerable<IEntityInstance> templateArguments,
            ObjectData thisObject, params ObjectData[] arguments)
        {
            // btw. arguments have to be given in exact order as parameters go

            if (targetFunc.Parameters.Any(it => it.IsVariadic))
                throw new Exception($"{ExceptionCode.SourceInfo()}");

            ObjectData this_ref = await prepareThisAsync(ctx, thisObject, $"{targetFunc}").ConfigureAwait(false);
            ObjectData[] args = await prepareArguments(ctx, targetFunc,
                // that is why this function does not handle variadic, it assumes single argument per parameter
                arguments.Select(it => ArgumentGroup.Single(it))).ConfigureAwait(false);
            SetupFunctionCallData(ref ctx, templateArguments, this_ref, args);
            ExecValue ret = await ExecutedAsync(targetFunc, ctx).ConfigureAwait(false);
            return ret;
        }
        private Task<ExecValue> createOption(ExecutionContext ctx, IEntityInstance optionType, Option<ObjectData> option)
        {
            return createObject(ctx, false, optionType,
                option.HasValue ? ctx.Env.OptionValueConstructor : ctx.Env.OptionEmptyConstructor,
                null,
                option.HasValue ? new[] { option.Value } : new ObjectData[] { });
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
            else if (owner_type == ctx.Env.Utf8StringType)
                return await executeNativeUtf8StringFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
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
            else if (owner_type == ctx.Env.CharType)
                return await executeNativeCharFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
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
                ObjectData enum_obj = await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, arg_obj.PlainValue).ConfigureAwait(false);
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

        // https://stackoverflow.com/a/15111719/210342
        private static IEnumerable<string> graphemeClusters(string s)
        {
            var enumerator = StringInfo.GetTextElementEnumerator(s);
            while (enumerator.MoveNext())
            {
                yield return (string)enumerator.Current;
            }
        }
        private static string reverseGraphemeClusters(string s)
        {
            return string.Join("", graphemeClusters(s).Reverse().ToArray());
        }

        private async Task<ExecValue> executeNativeCharFunctionAsync(ExecutionContext ctx, FunctionDefinition func,
            ObjectData thisValue)
        {
            char this_native = thisValue.NativeChar;

            if (func == ctx.Env.CharLengthGetter)
            {
                ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    (byte)Encoding.UTF8.GetByteCount($"{this_native}")).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func == ctx.Env.CharToString)
            {
                ObjectData result = await createStringAsync(ctx, $"{this_native}").ConfigureAwait(false);
                if (!ctx.Heap.TryInc(ctx, result, RefCountIncReason.NewString, $"{this_native}"))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                return ExecValue.CreateReturn(result);
            }
            else
                throw new NotImplementedException($"Function {func} is not implemented");
        }

        private static int[] toCodePoints(string str)
        {
            // https://stackoverflow.com/a/28155130/210342
            var codePoints = new List<int>(str.Length);
            for (int i = 0; i < str.Length; ++i)
            {
                codePoints.Add(Char.ConvertToUtf32(str, i));
                if (Char.IsHighSurrogate(str[i]))
                    i += 1;
            }

            return codePoints.ToArray();
        }

        private async Task<ExecValue> assignedNativeString(ExecutionContext ctx, ObjectData thisValue,string s)
        {
            ObjectData obj = await ObjectData.CreateInstanceAsync(ctx, ctx.Env.Utf8StringType.InstanceOf, s).ConfigureAwait(false);
            thisValue.Assign(obj);
            return ExecValue.CreateReturn(null);
        }

        private async Task<ExecValue> executeNativeUtf8StringFunctionAsync(ExecutionContext ctx, FunctionDefinition func,
            ObjectData thisValue)
        {
            string this_native = thisValue.NativeString;

            if (func == ctx.Env.Utf8StringCopyConstructor)
            {
                ObjectData arg_str_obj = ctx.FunctionArguments[0];
                if (!arg_str_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_str_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                string native_str_arg = arg_str_val.NativeString;

                return await assignedNativeString(ctx, thisValue, native_str_arg).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringRemove)
            {
                ObjectData arg_start_obj = ctx.FunctionArguments[0];
                int native_start_arg = (int)arg_start_obj.NativeNat;
                ObjectData arg_end_obj = ctx.FunctionArguments[1];
                int native_end_arg = (int)arg_end_obj.NativeNat;
                int native_len_arg = native_end_arg - native_start_arg;

                byte[] this_utf8 = Encoding.UTF8.GetBytes(this_native);
                string rest = Encoding.UTF8.GetString(this_utf8, 0,native_start_arg)
                    + Encoding.UTF8.GetString(this_utf8, native_start_arg+ native_len_arg,this_utf8.Length-(native_start_arg + native_len_arg));

                return await assignedNativeString(ctx, thisValue, rest).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringCountGetter)
            {
                int[] code_points = toCodePoints(this_native);
                ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    (UInt64)code_points.Length).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func == ctx.Env.Utf8StringLengthGetter)
            {
                int length = Encoding.UTF8.GetByteCount(this_native);
                ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                    (UInt64)length).ConfigureAwait(false);
                return ExecValue.CreateReturn(result);
            }
            else if (func == ctx.Env.Utf8StringTrimStart)
            {
                string trimmed = this_native.TrimStart();
                return await assignedNativeString(ctx, thisValue, trimmed).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringReverse)
            {
                // https://en.wikipedia.org/wiki/Combining_character
                string reversed = reverseGraphemeClusters(this_native);
                return await assignedNativeString(ctx, thisValue, reversed).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringTrimEnd)
            {
                string trimmed = this_native.TrimEnd();
                return await assignedNativeString(ctx, thisValue, trimmed).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringAtGetter)
            {
                ObjectData arg_idx_obj = ctx.FunctionArguments[0];
                int native_idx_arg = (int)arg_idx_obj.NativeNat;

                byte[] this_utf8 = Encoding.UTF8.GetBytes(this_native);
                string sub = Encoding.UTF8.GetString(this_utf8, native_idx_arg, this_utf8.Length - native_idx_arg);

                ObjectData obj_ch = await createCharAsync(ctx, sub[0]).ConfigureAwait(false);

                // indexer returns reference to an element value
                ObjectData obj_ref = await obj_ch.ReferenceAsync(ctx).ConfigureAwait(false);
                return ExecValue.CreateReturn(obj_ref);
            }
            else if (func == ctx.Env.Utf8StringSlice)
            {
                ObjectData arg_start_obj = ctx.FunctionArguments[0];
                int native_start_arg = (int)arg_start_obj.NativeNat;
                ObjectData arg_end_obj = ctx.FunctionArguments[1];
                int native_end_arg = (int)arg_end_obj.NativeNat;
                int native_len_arg = native_end_arg-native_start_arg;

                byte[] this_utf8 = Encoding.UTF8.GetBytes(this_native);
                string sub = Encoding.UTF8.GetString(this_utf8, native_start_arg, native_len_arg);

                ObjectData result = await createStringAsync(ctx, sub).ConfigureAwait(false);
                if (!ctx.Heap.TryInc(ctx, result, RefCountIncReason.NewString, sub))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                return ExecValue.CreateReturn(result);
            }
            else if (func == ctx.Env.Utf8StringConcat)
            {
                ObjectData arg_str_obj = ctx.FunctionArguments[0];
                if (!arg_str_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_str_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                string native_str_arg = arg_str_val.NativeString;

                string concatenated = this_native + native_str_arg;
                return await assignedNativeString(ctx, thisValue, concatenated).ConfigureAwait(false);
            }
            else if (func == ctx.Env.Utf8StringIndexOfString)
            {
                ObjectData arg_str_obj = ctx.FunctionArguments[0];
                if (!arg_str_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_str_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                string native_str_arg = arg_str_val.NativeString;
                ObjectData arg_idx_obj = ctx.FunctionArguments[1];
                int native_idx_arg = (int)arg_idx_obj.NativeNat;

                byte[] this_utf8 = Encoding.UTF8.GetBytes(this_native);
                string sub = Encoding.UTF8.GetString(this_utf8, native_idx_arg, this_utf8.Length - native_idx_arg);

                int idx = sub.IndexOf(native_str_arg);

                Option<ObjectData> index_obj;
                if (idx != -1)
                {
                    idx = native_idx_arg + Encoding.UTF8.GetByteCount(sub.Substring(0, idx));
                    index_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.SizeType.InstanceOf, (UInt64)idx)
                        .ConfigureAwait(false));
                }
                else
                    index_obj = new Option<ObjectData>();

                ExecValue opt_exec = await createOption(ctx, func.ResultTypeName.Evaluation.Components, index_obj).ConfigureAwait(false);
                if (opt_exec.IsThrow)
                    return opt_exec;

                return ExecValue.CreateReturn(opt_exec.ExprValue);
            }
            else if (func == ctx.Env.Utf8StringLastIndexOfChar)
            {
                ObjectData arg_char_obj = ctx.FunctionArguments[0];
                char native_char_arg = arg_char_obj.NativeChar;
                ObjectData arg_idx_obj = ctx.FunctionArguments[1];
                int native_idx_arg = (int)arg_idx_obj.NativeNat;

                byte[] this_utf8 = Encoding.UTF8.GetBytes(this_native);
                string sub = Encoding.UTF8.GetString(this_utf8, 0, native_idx_arg);

                int idx = sub.LastIndexOf(native_char_arg);

                Option<ObjectData> index_obj;
                if (idx != -1)
                {
                    idx = Encoding.UTF8.GetByteCount(this_native.Substring(0, idx));
                    index_obj = new Option<ObjectData>(await ObjectData.CreateInstanceAsync(ctx, ctx.Env.SizeType.InstanceOf, (UInt64)idx)
                    .ConfigureAwait(false));
                }
                else
                    index_obj = new Option<ObjectData>();

                ExecValue opt_exec = await createOption(ctx, func.ResultTypeName.Evaluation.Components, index_obj).ConfigureAwait(false);
                if (opt_exec.IsThrow)
                    return opt_exec;

                return ExecValue.CreateReturn(opt_exec.ExprValue);
            }
            else
            {
                ExecValue? result = await equalityTestAsync<string>(ctx, func, thisValue, heapArguments: true).ConfigureAwait(false);
                if (result.HasValue)
                    return result.Value;
                else
                    throw new NotImplementedException($"Function {func} is not implemented");
            }
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

                Option<ObjectData> opt_lines_obj;
                ObjectData chunk_ptr;

                if (lines == null)
                {
                    chunk_ptr = null;
                    opt_lines_obj = new Option<ObjectData>();
                }
                else
                {
                    IEntityInstance string_ptr_instance;
                    {
                        IEntityInstance ptr_iterable_instance = func.ResultTypeName.Evaluation.Components.Cast<EntityInstance>().TemplateArguments.Single();
                        IEntityInstance iterable_str_ptr_instance = ptr_iterable_instance.Cast<EntityInstance>().TemplateArguments.Single();
                        string_ptr_instance = iterable_str_ptr_instance.Cast<EntityInstance>().TemplateArguments.Single();
                    }

                    var lines_obj = new ObjectData[lines.Length];
                    int i = 0;
                    foreach (string s in lines)
                    {
                        ObjectData s_ptr = await createStringAsync(ctx, s).ConfigureAwait(false);
                        if (!ctx.Heap.TryInc(ctx, s_ptr, RefCountIncReason.FileLine, $"{filepath}"))
                            throw new Exception($"{ExceptionCode.SourceInfo()}");

                        lines_obj[i] = s_ptr;
                        ++i;
                    }

                    chunk_ptr = await createChunkOnHeap(ctx, string_ptr_instance, lines_obj).ConfigureAwait(false);

                    opt_lines_obj = new Option<ObjectData>(chunk_ptr);
                }


                ExecValue opt_exec = await createOption(ctx, func.ResultTypeName.Evaluation.Components, opt_lines_obj).ConfigureAwait(false);
                if (chunk_ptr != null)
                    ctx.Heap.TryRelease(ctx, chunk_ptr, null, false, RefCountDecReason.DroppingLocalPointer, "");

                if (opt_exec.IsThrow)
                    return opt_exec;
                ObjectData result = opt_exec.ExprValue;
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
                EntityInstance option_type = ctx.Env.OptionType.GetInstance(new[] { value_type }, 
                    overrideMutability: MutabilityOverride.None,
                    translation: null);
                ExecValue opt_exec = await createOption(ctx, option_type, received).ConfigureAwait(false);
                if (opt_exec.IsThrow)
                    return opt_exec;
                ObjectData result = opt_exec.ExprValue;

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
            else if (func.IsCopyInitConstructor(ctx.CreateBareComputation()))
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
