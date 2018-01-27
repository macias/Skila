using System;
using System.Collections.Generic;

namespace Skila.Language
{
#if DEBUG
    public sealed class DebugId
    {
        private static object threadLock = new object();
        private static Dictionary<Type, int> typedId = new Dictionary<Type, int>();

        public readonly int Id;

        public DebugId(Type type) 
        {
            this.Id = getId(type);
            if (Id == 1958)
            {
                ;
            }
        }

        private static int getId(Type type)
        {
            lock (threadLock)
            {
                int value;
                if (typedId.TryGetValue(type, out value))
                {
                    ++value;
                    typedId[type] = value;
                }
                else
                {
                    value = 0;
                    typedId.Add(type, value);
                }

                return value;
            }
        }

        public override string ToString()
        {
            return Id.ToString();
        }
    }
#endif
}
