namespace Skila.Interpreter
{
    internal enum RefCountIncReason
    {
        IncChunkOnHeap,
        CopyingChunkElem,
        SettingChunkElem,
        PrepareExitData,
        PrepareArgument,
        PrepareThis,
        Declaration,
        Assignment,
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