using System;
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
        private static async Task<ObjectData> createChunkOnHeap(ExecutionContext ctx, IEntityInstance elementType, 
            IEnumerable<ObjectData> elements)
        {
            ObjectData chunk_obj = await createChunk(ctx,
                ctx.Env.ChunkType.GetInstance(new[] { elementType }, MutabilityFlag.ConstAsSource, null),
                elements.ToArray()).ConfigureAwait(false);
            ObjectData chunk_ptr = await allocateOnHeapAsync(ctx,
                ctx.Env.Reference(chunk_obj.RunTimeTypeInstance, MutabilityFlag.ConstAsSource, null, viaPointer: true),
                chunk_obj).ConfigureAwait(false);
            if (!ctx.Heap.TryInc(ctx, chunk_ptr,RefCountIncReason.IncChunkOnHeap,  ""))
                throw new Exception($"{ExceptionCode.SourceInfo()}");

            return chunk_ptr;
        }

        private async Task<ExecValue> executeNativeChunkFunctionAsync(ExecutionContext ctx, FunctionDefinition func, ObjectData this_value)
        {
            if (func == ctx.Env.ChunkSizeConstructor)
            {
                IEntityInstance elem_type = this_value.RunTimeTypeInstance.TemplateArguments.Single();
                ObjectData size_obj = ctx.FunctionArguments.Single();
                var size = size_obj.NativeNat64;
                ObjectData[] chunk = (await Task.WhenAll(Enumerable.Range(0, (int)size).Select(_ => ObjectData.CreateEmptyAsync(ctx, elem_type))).ConfigureAwait(false)).ToArray();
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
                var size = size_obj.NativeNat64;
                ObjectData source_obj = ctx.FunctionArguments[1];
                if (!source_obj.TryDereferenceAnyOnce(ctx.Env, out ObjectData val_obj))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                Chunk source = val_obj.PlainValue.Cast<Chunk>();

                ObjectData[] chunk = new ObjectData[size];
                var copy_size = Math.Min(size, source.Count);
                for (UInt64 i = 0; i != copy_size; ++i)
                {
                    chunk[i] = source[i];
                    ctx.Heap.TryInc(ctx, source[i], RefCountIncReason.CopyingChunkElem, "");
                }
                for (var i = copy_size; i < size; ++i)
                {
                    chunk[i] = await ObjectData.CreateEmptyAsync(ctx, elem_type).ConfigureAwait(false);
                }
                this_value.Assign(await createChunk(ctx, this_value.RunTimeTypeInstance, chunk).ConfigureAwait(false));
                return ExecValue.CreateReturn(null);
            }
            else if (func == ctx.Env.ChunkAtSet)
            {
                ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                var idx = idx_obj.NativeNat64;
                Chunk chunk = this_value.PlainValue.Cast<Chunk>();
                ctx.Heap.TryRelease(ctx, chunk[idx], passingOutObject: null,isPassingOut: false, reason: RefCountDecReason.ReplacingChunkElem, callInfo: "");
                ObjectData arg_ref_object = ctx.GetArgument(func, NameFactory.PropertySetterValueParameter);
                // indexer takes reference to element
                if (!arg_ref_object.TryDereferenceAnyOnce(ctx.Env, out ObjectData arg_val))
                    throw new Exception($"{ExceptionCode.SourceInfo()}");
                if (!ctx.Heap.TryInc(ctx, arg_val, RefCountIncReason.SettingChunkElem, ""))
                    arg_val = arg_val.Copy();
                chunk[idx] = arg_val;
                return ExecValue.CreateReturn(null);
            }
            else if (func == ctx.Env.ChunkAtGet)
            {
                ObjectData idx_obj = ctx.GetArgument(func, NameFactory.IndexIndexerParameter);
                var idx = idx_obj.NativeNat64;
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
    }
}
