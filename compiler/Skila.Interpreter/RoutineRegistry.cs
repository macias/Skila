using NaiveLanguageTools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Skila.Interpreter
{
    sealed class RoutineRegistry
    {
        private readonly object threadLock = new object();

        private readonly HashSet<Task> tasks;
        private readonly List<Exception> errors;

        public RoutineRegistry()
        {
            this.tasks = new HashSet<Task>();
            this.errors = new List<Exception>();
        }

        public void CompleteWith(Task mainTask)
        {
            Task[] tt;
            lock (this.threadLock)
                tt = this.tasks.Concat(mainTask).ToArray();

            Task.WaitAll(tt);
            lock (this.threadLock)
            {
                this.errors.AddRange(tt.Where(it => it.IsCanceled || it.IsFaulted).Select(it => it.Exception));
                if (this.errors.Any())
                    throw new Exception("Routine failed");
            }
        }
        internal void Run(Task<ExecValue> routine)
        {
            lock (this.threadLock)
                tasks.Add(routine);

            routine.ContinueWith(t =>
            {
                lock (this.threadLock)
                {
                    if (t.IsCanceled || t.IsFaulted)
                        errors.Add(t.Exception);
                    tasks.Remove(routine);
                }
            });
        }
    }
}