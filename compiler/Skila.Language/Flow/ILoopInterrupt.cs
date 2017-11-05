namespace Skila.Language.Flow
{
    public interface ILoopInterrupt
    {
        IAnchor AssociatedLoop { get; }
        bool IsBreak { get; }
    }
}