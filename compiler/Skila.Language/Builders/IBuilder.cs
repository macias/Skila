namespace Skila.Language.Builders
{
    public interface IBuilder<T>
        where T : INode
    {
        T Build();
    }

}
