namespace Skila.Language
{
    public enum MutabilityModeOption
    {
        // there is a difference between mutating data and reassigning
        MutabilityAndAssignability,
        // mutability dictates everything, either variable can be reassigned/mutated, or not at all
        SingleMutability,
        // there is not such notion as const/mutable variable, only whether we can reassign it or not (like in C# with readonly)
        OnlyAssignability,
    }
}