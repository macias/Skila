namespace Skila.Language
{
    public interface IExpression : IEvaluable
    {
        // todo: 2018-12-01 -- remove IsDereferenced and rely on IsDereferencing instead since it allows sharing child node
        // since info about dereferencing sits in parent, not in child, but maybe it is not that universal, so setting date to future
        bool IsDereferenced { get; set; }
        bool IsDereferencing { get; set; }
        ExpressionReadMode ReadMode { get; }
        bool IsRead { get; set; }
        ExecutionFlow Flow { get; }

        bool IsReadingValueOfNode(IExpression node);
        bool IsLValue(ComputationContext ctx);
    }
}
