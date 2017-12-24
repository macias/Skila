using NaiveLanguageTools.Common;
using System.Threading.Tasks;

namespace Skila.Interpreter.Channels
{
    interface IChannel<T>
    {
        void Close();
        Task<Option<T>> ReceiveAsync();
        Task<bool> SendAsync(T value);
    }
}