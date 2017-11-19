using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using Skila.Language.Extensions;
using Skila.Language.Data;

namespace Skila.Language.Semantics
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed partial class VariableTracker
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        private int operationIdCounter;
        private ExecutionMode executionMode;
        private readonly LayerDictionary<VariableDeclaration, VariableInfo> variables;

        public VariableTracker()
        {
            this.variables = new LayerDictionary<VariableDeclaration, VariableInfo>();
            this.executionMode = ExecutionMode.Certain;
        }

        private VariableTracker(VariableTracker src)
        {
            this.variables = new LayerDictionary<VariableDeclaration, VariableInfo>();
            foreach (IEnumerable<VariableDeclaration> layer in src.variables.EnumerateLayers())
            {
                this.variables.PushLayer();
                foreach (VariableDeclaration decl in layer)
                    this.variables.Add(decl, src.variables[decl].Clone());
            }
            this.operationIdCounter = src.operationIdCounter;
            this.executionMode = src.executionMode;
        }

        public TrackingState StartFlow()
        {
            return new TrackingState(this.operationIdCounter, this.executionMode);
        }

        public VariableTracker Clone()
        {
            return new VariableTracker(this);
        }

        internal void Add(VariableDeclaration decl)
        {
            this.variables.Add(decl, new VariableInfo(
                decl.InitValue == null ? VariableState.NotInitialized : VariableState.Assigned, ++operationIdCounter));
        }

        internal void AddLayer(IScope scope)
        {
            if (scope != null)
                this.variables.PushLayer();
        }

        internal void Import(VariableTracker branch)
        {
            foreach (KeyValuePair<VariableDeclaration, VariableInfo> entry in branch.variables)
            {
                if (!this.variables.TryGetValue(entry.Key, out VariableInfo info))
                {
                    // this is import of DECLARATIONS+read from single branch (so we have to reset state to uninitialized)
                    VariableInfo clone = entry.Value.Clone();
                    clone.Assign(VariableState.NotInitialized, clone.DeclarationId);
                    this.variables.Add(entry.Key, clone);
                }
                else if (entry.Value.IsRead)
                {
                    if (entry.Key.DebugId.Id == 7211)
                    {
                        ;
                    }
                    this.variables[entry.Key].IsRead = true;
                }
            }
        }
        internal void Combine(IEnumerable<VariableTracker> branches)
        {
            HashSet<VariableDeclaration> set = null;
            // first merge the branches
            foreach (VariableTracker tracker in branches)
            {
                IEnumerable<VariableDeclaration> locally_set = tracker.variables
                    .Where(it => it.Value.State == VariableState.Assigned)
                    .Select(it => it.Key);

                if (set == null)
                    set = new HashSet<VariableDeclaration>(locally_set);
                else
                    set.IntersectWith(locally_set);
            }

            // ... then merge the outcome with current tracker
            if (set != null)
                foreach (VariableDeclaration decl in set)
                {
                    if (this.variables.TryGetValue(decl, out VariableInfo info))
                        this.variables[decl].Assign(VariableState.Assigned, ++operationIdCounter);
                }
        }

        internal void Assigned(IExpression lhs)
        {
            // even if we are in unreachable flow we still set the assignment state to maybe, here is why
            // if (...) then
            //   throw new Exception();
            //   x = 3;    // unreachable
            //   y = x;    // x is properly assigned
            // end         // x is not assigned after that
            // so setting "maybe" in unreachable flow on one hand saves us from multiple errors
            // on the other hand it is harmful for outer scope, because maybe-assignments 
            // will be handled properly anyway

            var state = this.executionMode == ExecutionMode.Certain ? VariableState.Assigned : VariableState.Maybe;

            VariableDeclaration decl = lhs.TryGetVariable();
            if (decl != null)
            {
                if (this.variables.TryGetValue(decl, out VariableInfo info) && info.State != VariableState.Assigned)
                    this.variables[decl].Assign(state, ++operationIdCounter);
            }
        }

        public bool TryCanRead(NameReference name, out VariableDeclaration varDeclaration)
        {
            if (name.DebugId.Id == 2494)
            {
                ;
            }
            if (name.Binding.Match.Target is VariableDeclaration decl)
            {
                varDeclaration = decl;

                if (this.variables.TryGetValue(decl, out VariableInfo info))
                {
                    if (decl.DebugId.Id == 7211)
                    {
                        ;
                    }
                    info.IsRead = true;
                    return info.State != VariableState.NotInitialized;
                }
            }
            else
                varDeclaration = null;

            return true;
        }

        public bool CanRead(VariableDeclaration decl)
        {
            VariableInfo info = this.variables[decl];
            info.IsRead = true;
            return info.State != VariableState.NotInitialized;
        }

        public override string ToString()
        {
            return $"{variables.Count} entries, {variables.Count(it => it.Value.State != VariableState.NotInitialized)} assigned";
        }

        internal void EndTracking(TrackingState trackState)
        {
            // revert all maybes into uninitialized
            foreach (KeyValuePair<VariableDeclaration, VariableInfo> entry in this.variables
                .Where(it => it.Value.State == VariableState.Maybe && it.Value.AssignmentId > trackState.OperationIdCounter).StoreReadOnly())
                this.variables[entry.Key].Assign(VariableState.NotInitialized, trackState.OperationIdCounter);

            this.executionMode = trackState.Mode;
        }

        public IEnumerable<VariableDeclaration> RemoveLayer()
        {
            return this.variables.PopLayer().Where(it => !it.Item2.IsRead && !it.Item2.IsCloned).Select(it => it.Item1);
        }

        internal void UpdateMode(ExecutionMode mode)
        {
            this.executionMode = this.executionMode.GetMoreUncertain(mode);
        }
    }

}