namespace Skila.Language.Builders
{
    public interface IBuilder<out T>
      //  where T : INode
    {
        T Build();
    }

}
