using System.Linq;

namespace Skila.Interpreter
{
    internal sealed class Chunk : ICopyableValue
    {
        private readonly ObjectData[] data;

        public ObjectData this[int idx]
        {
            get { return this.data[idx]; }
            set { this.data[idx] = value; }
        }

        public Chunk(ObjectData[] data)
        {
            this.data = data;
        }
        public ICopyableValue Copy()
        {
            return new Chunk(this.data.Select(it => it.Copy()).ToArray());
        }
    }
}