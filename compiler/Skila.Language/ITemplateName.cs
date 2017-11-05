namespace Skila.Language
{
    // name definition or name reference
    public interface ITemplateName : INode
    {
        string Name { get; }
        int Arity { get; }
    }

}
