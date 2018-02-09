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
        private async Task<ExecValue> executeNativeFunctionAsync(ExecutionContext ctx, FunctionDefinition func)
        {
            TypeDefinition owner_type = func.ContainingType();

            // meta-this is always passed as reference or pointer, so we can blindly dereference it
            ObjectData this_value = func.Modifier.HasStatic ? null : ctx.ThisArgument.DereferencedOnce();

            if (owner_type.Modifier.HasEnum)
            {
                if (func.IsInitConstructor()
                    && func.Parameters.Count == 1 && func.Parameters.Single().Name.Name == NameFactory.EnumConstructorParameter)
                {
                    this_value.Assign(ctx.FunctionArguments.Single());
                    return ExecValue.CreateReturn(null);
                }
                else if (func.Name.Name == NameFactory.EqualOperator)
                {
                    ObjectData arg = ctx.FunctionArguments.Single();
                    int this_int = this_value.PlainValue.Cast<int>();
                    int arg_int = arg.PlainValue.Cast<int>();
                    ExecValue result = ExecValue.CreateReturn(await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components, this_int == arg_int).ConfigureAwait(false));
                    return result;
                }
                else if (func.Name.Name == NameFactory.ConvertFunctionName)
                {
                    if (ctx.Env.IsIntType(func.ResultTypeName.Evaluation.Components))
                    {
                        ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components, this_value.PlainValue).ConfigureAwait(false);
                        return ExecValue.CreateReturn(result);
                    }
                    else
                        throw new NotImplementedException($"Enum func {func} is not implemented");
                }
                else
                    throw new NotImplementedException($"Function {func} is not implemented");
            }
            else if (owner_type == ctx.Env.UnitType)
            {
                if (func.IsDefaultInitConstructor())
                {
                    this_value.Assign(await ObjectData.CreateInstanceAsync(ctx, this_value.RunTimeTypeInstance, UnitType.UnitValue).ConfigureAwait(false));
                    return ExecValue.CreateReturn(null);
                }
                else
                    throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
            }
            else if (owner_type == ctx.Env.StringType)
            {
                if (func == ctx.Env.StringCountGetter)
                {
                    string native_object = this_value.PlainValue.Cast<string>();
                    ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                        native_object.Length).ConfigureAwait(false);
                    return ExecValue.CreateReturn(result);
                }
                else
                    throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
            }
            else if (owner_type == ctx.Env.FileType)
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
                            if (!ctx.Heap.TryInc(ctx, s_ptr, $"building chunk with file lines"))
                                throw new Exception($"{ExceptionCode.SourceInfo()}");

                            chunk[i] = s_ptr;
                            ++i;
                        }

                        ObjectData chunk_obj = await createChunk(ctx,
                            ctx.Env.ChunkType.GetInstance(new[] { string_ptr_instance },
                            MutabilityFlag.ConstAsSource, null),
                            chunk).ConfigureAwait(false);
                        ObjectData chunk_ptr = await allocateOnHeapAsync(ctx,
                            ctx.Env.Reference(chunk_obj.RunTimeTypeInstance, MutabilityFlag.ConstAsSource, null, viaPointer: true),
                            chunk_obj).ConfigureAwait(false);
                        if (!ctx.Heap.TryInc(ctx, chunk_ptr, $"chunk with file lines on heap"))
                            throw new Exception($"{ExceptionCode.SourceInfo()}");

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
            else if (owner_type == ctx.Env.ChunkType)
            {
                if (func == ctx.Env.ChunkSizeConstructor)
                {
                    IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                    ObjectData size_obj = ctx.FunctionArguments.Single();
                    int size = size_obj.PlainValue.Cast<int>();
                    ObjectData[] chunk = (await Task.WhenAll(Enumerable.Range(0, size).Select(_ => ObjectData.CreateEmptyAsync(ctx, elem_type))).ConfigureAwait(false)).ToArray();
                    this_value.Assign(await createChunk(ctx, this_value.RunTimeTypeInstance, chunk).ConfigureAwait(false));
                    return ExecValue.CreateReturn(null);
                }
                else if (func == ctx.Env.ChunkResizeConstructor)
                {
                    if (this_value.DebugId.Id == 184694)
                    {
                        ;
                    }
                    IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                    ObjectData size_obj = ctx.FunctionArguments[0];
                    int size = size_obj.PlainValue.Cast<int>();
                    ObjectData source_obj = ctx.FunctionArguments[1];
                    if (!source_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData val_obj))
                        throw new Exception($"{ExceptionCode.SourceInfo()}");
                    Chunk source = val_obj.PlainValue.Cast<Chunk>();

                    ObjectData[] chunk = new ObjectData[size];
                    int copy_size = Math.Min(size, source.Count);
                    for (int i = 0; i < copy_size; ++i)
                    {
                        chunk[i] = source[i];
                        ctx.Heap.TryInc(ctx, source[i], "copying chunk");
                    }
                    for (int i = copy_size; i < size; ++i)
                    {
                        chunk[i] = await ObjectData.CreateEmptyAsync(ctx, elem_type).ConfigureAwait(false);
                    }
                    this_value.Assign(await createChunk(ctx, this_value.RunTimeTypeInstance, chunk).ConfigureAwait(false));
                    return ExecValue.CreateReturn(null);
                }
                else if (func == ctx.Env.ChunkAtSet)
                {
                    ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                    int idx = idx_obj.PlainValue.Cast<int>();
                    Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                    ctx.Heap.TryRelease(ctx, chunk[idx], passingOutObject: null, callInfo: "replacing chunk elem");
                    ObjectData arg_ref_object = ctx.GetArgument(func, NameFactory.PropertySetterValueParameter);
                    // indexer takes reference to element
                    if (!arg_ref_object.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_val))
                        throw new Exception($"{ExceptionCode.SourceInfo()}");
                    if (!ctx.Heap.TryInc(ctx, arg_val, "setting chunk elem"))
                        arg_val = arg_val.Copy();
                    chunk[idx] = arg_val;
                    return ExecValue.CreateReturn(null);
                }
                else if (func == ctx.Env.ChunkAtGet)
                {
                    ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                    int idx = idx_obj.PlainValue.Cast<int>();
                    Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                    ObjectData obj_value = chunk[idx];
                    // indexer returns reference to an element value
                    ObjectData obj_ref = await obj_value.ReferenceAsync(ctx).ConfigureAwait(false);
                    return ExecValue.CreateReturn(obj_ref);
                }
                else if (func == ctx.Env.ChunkCount)
                {
                    Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                    ObjectData result = await ObjectData.CreateInstanceAsync(ctx, func.ResultTypeName.Evaluation.Components,
                        chunk.Count).ConfigureAwait(false);
                    return ExecValue.CreateReturn(result);
                }
                else
                    throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
            }
            else if (owner_type == ctx.Env.IntType)
            {
                return await executeNativeIntFunctionAsync(ctx, func, this_value).ConfigureAwait(false);
            }
            else if (owner_type == ctx.Env.IObjectType)
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
            else if (owner_type == ctx.Env.BoolType)
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
            else if (owner_type == ctx.Env.DateType)
            {
                if (func == ctx.Env.DateDayOfWeekGetter)
                {
                    ObjectData year_obj = this_value.GetField(ctx.Env.DateYearField);
                    int year = year_obj.PlainValue.Cast<int>();
                    ObjectData month_obj = this_value.GetField(ctx.Env.DateMonthField);
                    int month = month_obj.PlainValue.Cast<int>();
                    ObjectData day_obj = this_value.GetField(ctx.Env.DateDayField);
                    int day = day_obj.PlainValue.Cast<int>();

                    int day_of_week = (int)(new DateTime(year, month, day).DayOfWeek);

                    ObjectData ret_obj = await ObjectData.CreateInstanceAsync(ctx,
                        func.ResultTypeName.Evaluation.Components, day_of_week).ConfigureAwait(false);

                    return ExecValue.CreateReturn(ret_obj);
                }
                else
                {
                    throw new NotImplementedException($"{ExceptionCode.SourceInfo()}");
                }
            }
            else if (owner_type == ctx.Env.ChannelType)
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
            else
            {
                throw new NotImplementedException($"{owner_type}");
            }
        }
    }
}
