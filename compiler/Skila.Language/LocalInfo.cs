using Skila.Language.Extensions;
using System;
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

        /*public override bool Equals(object obj)
        {
            if (this.GetType() == obj?.GetType())
                return Equals(obj.Cast<LocalInfo>());
            else
                throw new Exception();
        }

        public bool Equals(LocalInfo other)
        {
            if (Object.ReferenceEquals(this, other))
                return true;
            else if (Object.ReferenceEquals(other, null))
                return false;

            return Object.Equals(this.Bindable, other.Bindable);
        }

        public override int GetHashCode()
        {
            return this.Bindable.GetHashCode();
        }*/
    }
}
