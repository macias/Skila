namespace Skila.Interpreter
{
    public partial struct ExecValue
    {
        private enum DataMode
        {
            Expression,
            Return,
            Throw,
            Recall
        }
    }
}