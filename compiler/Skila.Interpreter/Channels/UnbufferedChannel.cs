using System;
using NaiveLanguageTools.Common;
using System.Threading;
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
        private readonly AsyncAutoResetEvent writeGuard;
        private readonly AsyncAutoResetEvent readGuard;

        public UnbufferedChannel()
        {
            this.writeGuard = new AsyncAutoResetEvent(true);
            this.readGuard = new AsyncAutoResetEvent(false);
        }

        public void Dispose()
        {
        }

        public async Task<bool> SendAsync(T value)
        {
            await this.writeGuard.WaitOneAsync().ConfigureAwait(false);

            lock (this.threadLock)
            {
                if (this.isClosed)
                {
                    this.writeGuard.Set();
                    return false;
                }

                this.value = value;
                this.hasValue = true;
            }

            this.readGuard.Set();
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

            this.writeGuard.Set();
            return result;
        }

        public void Close()
        {
            lock (this.threadLock)
            {
                this.isClosed = true;
            }

            this.readGuard.Set();
            this.writeGuard.Set();
        }
    }
}