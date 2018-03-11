using Skila.Language;
using System;
using System.Linq;

namespace Skila.Interpreter
{
    // Chunk in Skila is value type, not reference one, so we need this wrapper for proper copy 

    internal sealed class Chunk : IInstanceValue
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(Chunk));
#endif
        private readonly ObjectData[] data;

        public UInt64 Count
        {
            get
            {
                return (UInt64)this.data.Length;
            }
        }

        public ObjectData this[UInt64 idx]
        {
            get { return this.data[validatedIndex((int) idx)]; }
            set { this.data[validatedIndex((int)idx)] = value; }
        }

        private int validatedIndex(int idx)
        {
            if (idx < 0 || idx >= this.data.Length)
                throw new IndexOutOfRangeException($"{ExceptionCode.SourceInfo()}");
            return idx;
        }
        public Chunk(ObjectData[] data)
        {
            if (this.DebugId==(43, 2))
            {
                ;
            }
            this.data = data;
        }
        public IInstanceValue Copy()
        {
            return new Chunk(this.data.Select(it => it.Copy()).ToArray());
        }
    }
}