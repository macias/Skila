namespace Skila.Language
{
    // name definition or name reference
    public interface ITemplateName : IOwnedNode
    {
        string Name { get; }
        int Arity { get; }
    }

}
