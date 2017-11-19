namespace Skila.Language
{
    public interface IExpression : IEvaluable
    {
        bool IsDereferenced { get; set; }
        ExpressionReadMode ReadMode { get; }
        bool IsRead { get; set; }
        ExecutionFlow Flow { get; }

        bool IsReadingValueOfNode(IExpression node);
        bool IsLValue(ComputationContext ctx);
    }
}
