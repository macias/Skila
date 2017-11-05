namespace Skila.Language.Semantics
{
    public sealed partial class VariableTracker
    {
        private enum VariableState
        {
            NotInitialized,
            Maybe,
            Assigned,
        }
    }
}