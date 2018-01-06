using System.Threading;

namespace Skila.Language
{
#if DEBUG
    public sealed class DebugId
    {
        private static int ID;
        public readonly int Id = Interlocked.Increment(ref ID);

        public DebugId()
        {
            if (Id ==     407)
            {
                ;
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
#endif
}
