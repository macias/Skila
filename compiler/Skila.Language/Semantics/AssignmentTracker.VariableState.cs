namespace Skila.Language.Semantics
{
    public sealed partial class AssignmentTracker
    {
        private enum VariableState
        {
            NotInitialized,
            Assigned,
        }
    }
}