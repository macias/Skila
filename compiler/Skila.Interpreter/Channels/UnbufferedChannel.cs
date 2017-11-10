using System;
using NaiveLanguageTools.Common;
using System.Threading;

namespace Skila.Interpreter.Channels
{
    sealed class UnbufferedChannel<T> : IDisposable, IChannel<T>
    {
        private readonly object threadLock = new object();

        bool isClosed;
        bool hasValue;
        T value;
        private readonly AutoResetEvent beforeWriteGuard;
        private readonly AutoResetEvent afterWriteGuard;
        private readonly AutoResetEvent readGuard;

        public UnbufferedChannel()
        {
            this.beforeWriteGuard = new AutoResetEvent(true);
            this.afterWriteGuard = new AutoResetEvent(false);
            this.readGuard = new AutoResetEvent(false);
        }

        public void Dispose()
        {
            this.beforeWriteGuard.Dispose();
            this.afterWriteGuard.Dispose();
            this.readGuard.Dispose();
        }

        public bool Send(T value)
        {
            this.beforeWriteGuard.WaitOne();

            lock (this.threadLock)
            {
                if (this.isClosed)
                {
                    this.beforeWriteGuard.Set();
                    return false;
                }

                this.value = value;
                this.hasValue = true;
            }

            this.readGuard.Set();
            this.afterWriteGuard.WaitOne();
            this.beforeWriteGuard.Set();

            return true;
        }

        public Option<T> Receive()
        {
            Option<T> result;

            this.readGuard.WaitOne();
            lock (this.threadLock)
            {
                if (this.isClosed && !this.hasValue)
                {
                    this.readGuard.Set();
                    return new Option<T>();
                }

                result = new Option<T>(this.value);
                this.hasValue = false;
            }

            this.afterWriteGuard.Set();
            return result;
        }

        public void Close()
        {
            lock (this.threadLock)
            {
                this.isClosed = true;
            }

            this.readGuard.Set();
            this.beforeWriteGuard.Set();
        }
    }
}