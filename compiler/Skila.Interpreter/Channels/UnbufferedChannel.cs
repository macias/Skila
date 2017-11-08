using System;
using NaiveLanguageTools.Common;
using Skila.Interpreter.Tools;
using System.Threading.Tasks;

namespace Skila.Interpreter.Channels
{
    sealed class UnbufferedChannel<T> : IDisposable, IChannel<T>
    {
        private readonly object threadLock = new object();

        bool isClosed;
        bool hasValue;
        T value;
        private readonly AsyncAutoResetEvent beforeWriteGuard;
        private readonly AsyncAutoResetEvent afterWriteGuard;
        private readonly AsyncAutoResetEvent readGuard;

        public UnbufferedChannel()
        {
            this.beforeWriteGuard = new AsyncAutoResetEvent(true);
            this.afterWriteGuard = new AsyncAutoResetEvent(false);
            this.readGuard = new AsyncAutoResetEvent(false);
        }

        public void Dispose()
        {
        }

        public async Task<bool> SendAsync(T value)
        {
            await this.beforeWriteGuard.WaitOneAsync().ConfigureAwait(false);

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
            await this.afterWriteGuard.WaitOneAsync().ConfigureAwait(false);
            this.beforeWriteGuard.Set();

            return true;
        }

        public async Task<Option<T>> ReceiveAsync()
        {
            Option<T> result;

            await this.readGuard.WaitOneAsync().ConfigureAwait(false);
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