using System;
using System.Linq;

namespace Skila.Interpreter
{
    // Chunk in Skila is value type, not reference one, so we need this wrapper for proper copy 

    internal sealed class Chunk : IInstanceValue
    {
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
            get { return this.data[idx]; }
            set { this.data[idx] = value; }
        }

        public Chunk(ObjectData[] data)
        {
            this.data = data;
        }
        public IInstanceValue Copy()
        {
            return new Chunk(this.data.Select(it => it.Copy()).ToArray());
        }
    }
}