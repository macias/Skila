using System;

namespace Skila.Interpreter
{
    [Flags]
    internal enum RefCountIncReason
    {
        IncChunkOnHeap = 1 << 0,
        CopyingChunkElem = 1 << 1,
        SettingChunkElem = 1 << 2,
        PrepareExitData = 1 << 3,
        PrepareArgument = 1 << 4,
        PrepareThis = 1 << 5,
        Declaration = 1 << 6,
        Assignment = 1 << 7,
        FileLine = 1 << 8,
        IncField = 1 << 9,
        StoringLocalPointer = 1 << 10,
        ThrowingException = 1 << 11,
        NewString = 1 << 12,
        CommandLine = 1 << 13
    }

    [Flags]
    internal enum RefCountDecReason
    {
        ReplacingChunkElem = 1 << 0,
        ReleaseExceptionFromMain = 1 << 1,
        UnwindingStack = 1 << 2,
        AssignmentLhsDrop = 1 << 3,
        FreeChunkElem = 1 << 4,
        FreeField = 1 << 5,
        DropOnCallResult = 1 << 6,
        DroppingLocalPointer = 1 << 7,
        CommandLine = 1 << 8,
        DeconstructingVariadic = 1 << 9
    }
}