namespace Skila.Language.Semantics
{
    public struct TrackingState
    {
        public int OperationIdCounter { get; }
        public ExecutionMode Mode { get; }

        public TrackingState(int opIdCounter, ExecutionMode mode)
        {
            this.OperationIdCounter = opIdCounter;
            this.Mode = mode;
        }
    }
}