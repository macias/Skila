namespace Skila.Language.Entities
{
    public interface IFunctionOutcome
    {
        INameReference ResultTypeName { get; }
        ExpressionReadMode CallMode { get; }
    }
}