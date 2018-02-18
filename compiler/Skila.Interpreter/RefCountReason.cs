namespace Skila.Interpreter
{
    internal enum RefCountIncReason
    {
        IncChunkOnHeap,
        CopyingChunkElem,
        SettingChunkElem,
        FuncCallArgPreparation,
        PrepareExitData,
        FuncCallPrepareThis,
        DeclAssign,
        FileLine,
        IncField,
        BuildingRegexCaptures
    }

    internal enum RefCountDecReason
    {
        ReplacingChunkElem,
        ReleaseExceptionFromMain,
        UnwindingStack,
        AssignmentLhsDrop,
        FreeChunkElem,
        FreeField,
        DropOnCallResult,
        BuildingRegexCaptures
    }
}