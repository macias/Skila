namespace Skila.Language
{
    public sealed class LocalInfo
    {
        public ILocalBindable Bindable { get; }
        public bool Used { get; set; }

        public LocalInfo(ILocalBindable bindable)
        {
            this.Bindable = bindable;
        }
    }
}
