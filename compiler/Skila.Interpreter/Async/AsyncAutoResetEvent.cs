﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skila.Interpreter.Async
{
    // https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-2-asyncautoresetevent/
    // the only addition is constructor with initial state

    public sealed class AsyncAutoResetEvent
    {
        private readonly static Task s_completed = Task.FromResult(true);
        private readonly Queue<TaskCompletionSource<bool>> m_waits = new Queue<TaskCompletionSource<bool>>();
        private bool m_signaled;

        public AsyncAutoResetEvent(bool initialState)
        {
            this.m_signaled = initialState;
        }

        public Task WaitOneAsync()
        {
            lock (m_waits)
            {
                if (m_signaled)
                {
                    m_signaled = false;
                    return s_completed;
                }
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    m_waits.Enqueue(tcs);
                    return tcs.Task;
                }
            }
        }

        public void Set()
        {
            TaskCompletionSource<bool> toRelease = null;
            lock (m_waits)
            {
                if (m_waits.Count > 0)
                    toRelease = m_waits.Dequeue();
                else if (!m_signaled)
                    m_signaled = true;
            }

            if (toRelease != null)
                toRelease.SetResult(true);
        }
    }
}