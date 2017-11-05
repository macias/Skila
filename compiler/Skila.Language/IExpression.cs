namespace Skila.Language
{
    public interface IExpression : IEvaluable
    {
        bool IsDereferenced { get; set; }
        ExpressionReadMode ReadMode { get; }
        ExecutionFlow Flow { get; }
        bool IsRead { get; set; }

        bool IsLValue(ComputationContext ctx);
        bool IsReadingValueOfNode( IExpression node);
    }
}
