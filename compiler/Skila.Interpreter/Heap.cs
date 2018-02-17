using System;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Linq;
using Skila.Language.Entities;

namespace Skila.Interpreter
{
    // the idea is as follows
    // assigning and variable declarations increment ref counters
    // on block exit all variables are removed, so ref counts are decremented
    // however two tricky parts are exiting blocks -- in this case given pointer can be passed out from block
    // so it ref count is decremented but not removed
    // and on function return (unlike block) we cannot check if the value will be used or not, so we always
    // increment ref count to prevent releasing the memory, on function call though we check if its value is used or not
    // if not, we decrement the ref count
    internal sealed class Heap
    {
        private readonly object threadLock = new object();

        // it is legal to have entry with 0 count (it happens on alloc and also on passing out pointers from block expressions)
        private readonly Dictionary<ObjectData, int> refCounts;
        private const int debugTraceId = -1;
        private int debugActionCount = 0;

        // we track host (C#) diposables created during interpretation just to check on exit if we cleaned all of them
        private int hostDisposables;

        public bool IsClean
        {
            get
            {
                return !this.refCounts.Any() && this.hostDisposables == 0;
            }
        }

        public Heap()
        {
            this.refCounts = new Dictionary<ObjectData, int>(ReferenceEqualityComparer<ObjectData>.Instance);
        }

        internal void Allocate(ObjectData obj)
        {
            if (obj.DebugId.Id == 8758)
            {
                ;
            }

            if (obj.DebugId.Id == debugTraceId)
            {
                print(0, 0, "", "Allocating object");
            }

            lock (this.threadLock)
            {
                this.refCounts.Add(obj, 0);
            }
        }

        internal bool TryRelease(ExecutionContext ctx, ObjectData releasingObject,
            // the object which is passed out of the block 
            ObjectData passingOutObject, bool isPassingOut,RefCountDecReason reason, string callInfo)
        {
            if (releasingObject == passingOutObject)
                isPassingOut = true;

            if (!ctx.Env.IsPointerOfType(releasingObject.RunTimeTypeInstance))
            {
                if (!ctx.Env.IsReferenceOfType(releasingObject.RunTimeTypeInstance))
                {
                    if (releasingObject.DebugId.Id == debugTraceId)
                        print(0, -1, $"VAL-DEL{(isPassingOut ? " / OUT" : "")}", callInfo);
                    // removing valued-objects
                    freeObjectData(ctx, releasingObject, passingOutObject, isPassingOut, false, $"as value {callInfo}");
                }
                return false;
            }

            releasingObject = releasingObject.DereferencedOnce();
            // todo: after adding nulls to Skila remove this condition
            if (releasingObject == null)
                return false;

            if (releasingObject.DebugId.Id == 183100)
            {
                ;
            }

            int count;

            lock (this.threadLock)
            {
                if (!this.refCounts.TryGetValue(releasingObject, out count))
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

                --count;
                if (count < 0)
                    throw new Exception($"Internal error {ExceptionCode.SourceInfo()}");

                if (count == 0 && !isPassingOut)
                {
                    this.refCounts.Remove(releasingObject);
                    freeObjectData(ctx, releasingObject, passingOutObject,isPassingOut, true, callInfo);
                }
                else
                    this.refCounts[releasingObject] = count;
            }

            if (releasingObject.DebugId.Id == debugTraceId)
            {
                if (debugActionCount == 15)
                {
                    ;
                }
                print(count, -1, $"DEC{(isPassingOut ? "/OUT" : "")}", $"{reason} {callInfo}");
            }

            return true;
        }

        private void freeObjectData(ExecutionContext ctx, ObjectData obj, ObjectData passingOut, bool isPassingOut, bool destroy, string callInfo)
        {
            lock (this.threadLock)
            {
                if (obj.Free(ctx, passingOut, isPassingOut, destroy, callInfo))
                    --this.hostDisposables;
            }
        }

        internal bool TryIncWithNested(ExecutionContext ctx, ObjectData objData, RefCountIncReason reason, string callInfo)
        {
            if (ctx.Env.IsPointerOfType(objData.RunTimeTypeInstance))
                return TryInc(ctx, objData, reason, callInfo);
            else if (!ctx.Env.IsReferenceOfType(objData.RunTimeTypeInstance))
            {
                foreach (KeyValuePair<VariableDeclaration, ObjectData> field in objData.Fields)
                {
                    ctx.Heap.TryIncWithNested(ctx, field.Value, reason: RefCountIncReason.IncField, callInfo: callInfo);
                }
            }

            return false;
        }

        internal bool TryInc(ExecutionContext ctx, ObjectData pointerObject, RefCountIncReason reason, string callInfo)
        {
            if (!ctx.Env.IsPointerOfType(pointerObject.RunTimeTypeInstance))
                return false;

            pointerObject = pointerObject.DereferencedOnce();

            if (pointerObject == null) // null pointer
                return false;

            if (pointerObject.DebugId.Id == 183067)
            {
            }

            int count;

            lock (this.threadLock)
            {
                count = this.refCounts[pointerObject] + 1;
                this.refCounts[pointerObject] = count;
            }

            if (pointerObject.DebugId.Id == debugTraceId)
            {
                if (count == 4)
                {
                    ;
                }
                print(count, +1, "INC", $"{reason} {callInfo}");
            }

            return true;
        }

        public override string ToString()
        {
            return this.refCounts.Select(it => $"{it.Key} #{it.Value}").Join(", ");
        }

        internal void TryAddDisposable<T>(T obj)
        {
            if (obj is IDisposable)
                lock (this.threadLock)
                    ++this.hostDisposables;
        }
        private void print(int count, int change, string operation, string comment)
        {
            if (debugActionCount == 14)
            {
                ;
            }
            string color = (change == 0 ? "black" : change > 0 ? "green" : "red");
            int margin = count;
            Console.WriteLine($"<p style=\"color: {color}; padding-left:{margin * 2}em\">{debugActionCount++} <b>{operation} {count}</b> {comment}</p>");
        }

    }
}
