using System;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using System.Linq;

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

        // we track host (C#) diposables created during interpretation just to check on exit if we cleaned all of them
        private int hostDisposables;

        public bool IsClean
        {
            get
            {
                lock (this.threadLock)
                {
                    return !this.refCounts.Any() && this.hostDisposables == 0;
                }
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

            lock (this.threadLock)
            {
                this.refCounts.Add(obj, 0);
            }
        }

        internal bool TryDec(ExecutionContext ctx, ObjectData obj, bool passingOut)
        {
            if (!ctx.Env.IsPointerOfType(obj.RunTimeTypeInstance))
                return false;

            obj = obj.Dereference();

            // todo: after adding nulls to Skila remove this condition
            if (obj == null)
                return false;

            if (obj.DebugId.Id == 8758)
            {
                ;
            }

            lock (this.threadLock)
            {
                int count;
                if (!this.refCounts.TryGetValue(obj, out count))
                    throw new Exception("Internal error");

                --count;
                if (count < 0)
                    throw new Exception("Internal error");

                if (count == 0 && !passingOut)
                {
                    this.refCounts.Remove(obj);
                    foreach (ObjectData field_obj in obj.Fields)
                        // locks are re-entrant, so recursive call is OK here
                        TryDec(ctx, field_obj, passingOut: false);
                    if (obj.Dispose())
                        --this.hostDisposables;
                }
                else
                    this.refCounts[obj] = count;
            }

            return true;
        }

        internal void TryInc(ExecutionContext ctx, ObjectData obj)
        {
            if (!ctx.Env.IsPointerOfType(obj.RunTimeTypeInstance))
                return;

            obj = obj.Dereference();

            if (obj == null) // null pointer
                return;

            if (obj.DebugId.Id == 8758)
            {
                ;
            }

            lock (this.threadLock)
            {
                this.refCounts[obj] = this.refCounts[obj] + 1;
            }
        }

        internal void TryAddDisposable<T>(T obj)
        {
            if (obj is IDisposable)
                lock (this.threadLock)
                    ++this.hostDisposables;
        }
    }
}
