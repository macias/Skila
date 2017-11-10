using NaiveLanguageTools.Common;
using System.Threading.Tasks;

namespace Skila.Interpreter.Channels
{
    interface IChannel<T>
    {
        void Close();
        Option<T> Receive();
        bool Send(T value);
    }
}