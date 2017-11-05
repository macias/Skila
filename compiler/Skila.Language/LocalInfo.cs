namespace Skila.Language
{
    public sealed class LocalInfo
    {
        public IBindable Bindable { get; }
        public bool Used { get; set; }

        public LocalInfo(IBindable bindable)
        {
            this.Bindable = bindable;
        }
    }
}
