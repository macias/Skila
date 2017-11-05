namespace Skila.Interpreter.Channels
{
    static class Channel
    {
        public static IChannel<T> Create<T>()
        {
            return new UnbufferedChannel<T>();
        }
    }
}