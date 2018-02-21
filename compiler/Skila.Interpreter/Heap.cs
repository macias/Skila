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
                print(0, 0, "ALLOC", $"Allocating object", $"{obj}");
            }

            lock (this.threadLock)
            {
                this.refCounts.Add(obj, 0);
            }
        }

        internal bool TryRelease(ExecutionContext ctx, ObjectData releasingObject,
            // the object which is passed out of the block 
            ObjectData passingOutObject, bool isPassingOut, RefCountDecReason reason, string comment)
        {
            if (releasingObject == passingOutObject)
                isPassingOut = true;

            if (!ctx.Env.IsPointerOfType(releasingObject.RunTimeTypeInstance))
            {
                if (!ctx.Env.IsReferenceOfType(releasingObject.RunTimeTypeInstance))
                {
                    if (releasingObject.DebugId.Id == debugTraceId)
                        print(0, -1, $"VAL-DEL{(isPassingOut ? " / OUT" : "")}", $"{reason}", comment);
                    // removing valued-objects
                    freeObjectData(ctx, releasingObject, passingOutObject, isPassingOut, reason, false, $"as value {comment}");
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
                    freeObjectData(ctx, releasingObject, passingOutObject, isPassingOut,reason, true, comment);
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
                print(count, -1, $"DEC{(isPassingOut ? "/OUT" : "")}", $"{reason}", comment);
            }

            return true;
        }

        private void freeObjectData(ExecutionContext ctx, ObjectData obj, ObjectData passingOut, bool isPassingOut,
            RefCountDecReason reason, bool destroy, string comment)
        {
            lock (this.threadLock)
            {
                if (obj.Free(ctx, passingOut, isPassingOut, reason, destroy, comment))
                    --this.hostDisposables;
            }
        }

        internal bool TryInc(ExecutionContext ctx, ObjectData objData, RefCountIncReason reason, string comment)
        {
            if (ctx.Env.IsPointerOfType(objData.RunTimeTypeInstance))
                return TryIncPointer(ctx, objData, reason, comment);
            else if (!ctx.Env.IsReferenceOfType(objData.RunTimeTypeInstance))
            {
                foreach (KeyValuePair<VariableDeclaration, ObjectData> field in objData.Fields)
                {
                    ctx.Heap.TryInc(ctx, field.Value, RefCountIncReason.IncField | reason, $"{comment}");
                }
            }

            return false;
        }

        internal bool TryIncPointer(ExecutionContext ctx, ObjectData pointerObject, RefCountIncReason reason, string comment)
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
                print(count, +1, "INC", $"{reason}", comment);
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
        private void print(int count, int change, string operation, string reason, string comment)
        {
            if (debugActionCount == 7 || debugActionCount==8)
            {
                ;
            }
            string color = (change == 0 ? "black" : change > 0 ? "green" : "red");
            int margin = count;
            Console.WriteLine($"<p style=\"padding-left:{margin * 2}em\"><span style=\"color: {color};\">{debugActionCount++} <b>{operation} {count}</b></span> {reason} <i>{comment}</i></p>");
        }

    }
}
