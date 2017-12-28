namespace Skila.Language
{
    public interface IFunctionExit : IFlowJump 
    {
         IExpression Expr { get; }
    }

}
