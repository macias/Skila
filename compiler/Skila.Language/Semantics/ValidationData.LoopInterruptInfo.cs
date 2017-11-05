using Skila.Language.Flow;
using System;
using System.Linq;

namespace Skila.Language.Semantics
{
    public sealed partial class ValidationData
    {
        private sealed class LoopInterruptInfo
        {
            // 0 -- root
            public int JumpReach { get; }
            public ILoopInterrupt Interrupt { get; }
            private ExecutionMode mode;
            public ExecutionMode Mode
            {
                get { return mode; }
                set
                {
                    if (value == ExecutionMode.Certain)
                        throw new ArgumentException("Not possible");
                    this.mode = value;
                }
            }

            public LoopInterruptInfo(ILoopInterrupt interrupt, ExecutionMode mode)
            {
                this.Interrupt = interrupt;
                this.Mode = mode;
                // we increase the jump reach to make room for "break" case
                // we multiply by two, to have easy distinction between continue and break
                this.JumpReach = (interrupt.AssociatedLoop.EnclosingScopesToRoot().Count() + 1) * 2;
                // "break" reaches further so we decrease the reach
                if (this.Interrupt.IsBreak)
                    --this.JumpReach;
            }

            public LoopInterruptInfo(LoopInterruptInfo src)
            {
                this.JumpReach = src.JumpReach;
                this.Interrupt = src.Interrupt;
                this.Mode = src.Mode;
            }

            internal LoopInterruptInfo Clone()
            {
                return new LoopInterruptInfo(this);
            }
            public override string ToString()
            {
                return $"{Mode}";
            }
        }
    }
   
}