namespace Skila.Language
{
    public enum MutabilityModeOption
    {
        // there is a difference between mutating data and reassigning
        MutabilityAndAssignability,
        // mutability dictates everything, either variable can be reassigned/mutated, or not at all
        // this mode is broken, because:
        // * it does not work with setter/getter notion, we could get a ref to mutable
        //   data, dereference it and reassign new value, all valid, but setter would not be triggered
        // * consumer (callee funtion) of the mutable could either set it or change it, so the caller
        //   could not pass safely const data for reassigning because there would be no notion of 
        //   such distinction (by design)
        // todo: redesign/remove it
        SingleMutability,
        // there is not such notion as const/mutable variable, only whether we can reassign it or not (like in C# with readonly)
        OnlyAssignability,
    }
}