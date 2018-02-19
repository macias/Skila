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
        StoringLocalPointer,
        ThrowingException
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
        DroppingLocalPointer
    }
}