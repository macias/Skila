﻿namespace Skila.Language
{
#if DEBUG
    public sealed class DebugId
    {
        private static int ID;
        public readonly int Id = ID++;

        public DebugId()
        {
            if (Id == 2799)
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
