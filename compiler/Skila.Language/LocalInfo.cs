using System.Diagnostics;

namespace Skila.Language
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class LocalInfo
    {
        public ILocalBindable Bindable { get; }
        public bool Used { get; set; }
        public bool Read { get; set; }

        public LocalInfo(ILocalBindable bindable)
        {
            this.Bindable = bindable;
        }

        public override string ToString()
        {
            return $"{Bindable} {(Used?"u":"")} {(Read?"r":"")}";
        }
    }
}
