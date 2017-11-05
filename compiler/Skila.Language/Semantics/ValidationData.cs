using NaiveLanguageTools.Common;
using Skila.Language.Comparers;
using Skila.Language.Flow;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Skila.Language.Semantics
{
    public sealed partial class ValidationData
    {
        private bool hasExit;
        // from innermost to outmost scope
        private Dictionary<ILoopInterrupt, LoopInterruptInfo> interruptions;

        // given path of execution is terminated for sure 
        public bool IsTerminated => this.interruptions.Any(it => it.Value.Mode == ExecutionMode.Unreachable) || this.hasExit;
        public bool UnreachableCodeFound { get; set; }

        internal static ValidationData Create()
        {
            return new ValidationData() { interruptions = new Dictionary<ILoopInterrupt, LoopInterruptInfo>(LoopInterruptComparer.Instance) };
        }

        private ValidationData()
        {

        }
        private ValidationData(ValidationData src) : this()
        {
            this.hasExit = src.hasExit;
            this.interruptions = src.interruptions.ToDictionary(it => it.Key, it => it.Value.Clone(), LoopInterruptComparer.Instance);
            this.UnreachableCodeFound = src.UnreachableCodeFound;
        }

        internal void AddInterruption(ILoopInterrupt interrupt)
        {
            if (interrupt.AssociatedLoop == null) // invalid interrupt, ignore
                return;

            LoopInterruptInfo exit;
            if (this.interruptions.TryGetValue(interrupt, out exit))
                exit.Mode = ExecutionMode.Unreachable;
            else
                this.interruptions.Add(interrupt, new LoopInterruptInfo(interrupt, ExecutionMode.Unreachable));
        }

        internal void RemoveInterruptionFor(IAnchor loop, bool isBreak)
        {
            foreach (var entry in this.interruptions.ToArray())
                if (entry.Key.AssociatedLoop == loop && entry.Key.IsBreak == isBreak)
                    this.interruptions.Remove(entry.Key);
        }

        internal ExecutionMode GetMode()
        {
            if (this.IsTerminated)
                return ExecutionMode.Unreachable;

            if (this.interruptions.Any())
                return ExecutionMode.Maybe;

            return ExecutionMode.Certain;
        }

        internal void AddStep(ValidationData src)
        {
            if (src.hasExit)
                this.hasExit = true;

            combineInterruptions(src);
        }

        private void combineInterruptions(ValidationData src)
        {
            if (src.UnreachableCodeFound)
                this.UnreachableCodeFound = true;

            foreach (LoopInterruptInfo src_exit in src.interruptions.Values)
            {
                LoopInterruptInfo jump;
                if (this.interruptions.TryGetValue(src_exit.Interrupt, out jump))
                    jump.Mode = jump.Mode.GetMoreUncertain(src_exit.Mode);
                else
                    this.interruptions.Add(src_exit.Interrupt, src_exit);
            }
        }

        internal void Combine(IReadOnlyList<ValidationData> branches)
        {
            foreach (ValidationData val in branches)
                val.lowerModes();

            var current = branches[0]; // Validation is a struct, so we have to extract&store it to change it
            if (branches.Count == 2)
            {
                current.hasExit = current.hasExit && branches[1].hasExit;
                overlapInterruptions(current.interruptions,branches[1].interruptions);
                overlapInterruptions(branches[1].interruptions, current.interruptions);
                current.combineInterruptions(branches[1]);
            }
            else if (branches.Count == 1)
                current.hasExit = false;
            else
                throw new Exception("Not possible");

            this.AddStep(current);
        }

        private static void overlapInterruptions(Dictionary<ILoopInterrupt, LoopInterruptInfo> interruptionsA, 
            Dictionary<ILoopInterrupt, LoopInterruptInfo> interruptionsB)
        {
            // if we have two overlapping "maybe" interruptions shorter interruption is converted to unreachable
            // because both branches will jump over given part of the code
            foreach (LoopInterruptInfo a_jump in interruptionsA.Values)
                foreach (LoopInterruptInfo b_jump in interruptionsB.Values.Where(j => a_jump.JumpReach <= j.JumpReach))
                    b_jump.Mode = ExecutionMode.Unreachable;
        }

        private void lowerModes()
        {
            foreach (LoopInterruptInfo exit in this.interruptions.Values)
                if (exit.Mode == ExecutionMode.Unreachable)
                    exit.Mode = ExecutionMode.Maybe;
        }

        internal void AddExit()
        {
            this.hasExit = true;
        }

        internal ValidationData Clone()
        {
            return new ValidationData(this);
        }

        public override string ToString()
        {
            return (IsTerminated ? "terminated" : "flowing") + " with " + (hasExit ? "exit" : "") + " " + String.Join(", ", interruptions.Values);
        }
    }
}