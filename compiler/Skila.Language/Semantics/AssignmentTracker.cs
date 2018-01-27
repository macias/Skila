using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using Skila.Language.Extensions;
using Skila.Language.Data;
using NaiveLanguageTools.Common;
using Skila.Language.Comparers;

namespace Skila.Language.Semantics
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed partial class AssignmentTracker : INameRegistry
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(AssignmentTracker));
#endif

        private int operationIdCounter;
        private ExecutionMode executionMode;
        private readonly ILayerDictionary<VariableDeclaration, VariableInfo> variables;
        private readonly bool shadowing;

        private static IEqualityComparer<VariableDeclaration> variableDeclarationComparer;

        static AssignmentTracker()
        {
            IEqualityComparer<ITemplateName> name_comparer = EntityNameArityComparer.Instance;
            variableDeclarationComparer = EqualityComparer.Create<VariableDeclaration>(
                (decl1, decl2) => name_comparer.Equals(decl1.Name, decl2.Name), 
                decl => name_comparer.GetHashCode(decl.Name));
        }
        public AssignmentTracker(bool shadowing)
        {
            this.shadowing = shadowing;
            this.variables = LayerDictionary.Create<VariableDeclaration, VariableInfo>(shadowing,variableDeclarationComparer);
            this.executionMode = ExecutionMode.Certain;
        }

        private AssignmentTracker(AssignmentTracker src)
        {
            this.shadowing = src.shadowing;
            this.variables = LayerDictionary.Create<VariableDeclaration, VariableInfo>(src.shadowing, variableDeclarationComparer);

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

        public AssignmentTracker Clone()
        {
            return new AssignmentTracker(this);
        }

        internal void Add(VariableDeclaration decl)
        {
            if (decl.DebugId.Id == 819)
            {
                ;
            }
            this.variables.Add(decl, new VariableInfo(
                decl.InitValue != null ? VariableState.Assigned : VariableState.NotInitialized, ++operationIdCounter));
        }

        internal void Add(IEnumerable<VariableDeclaration> decls)
        {
            decls.ForEach(it => this.Add(it));
        }

        public void AddLayer(IScope scope)
        {
            if (scope != null)
                this.variables.PushLayer();
        }

        internal void Import(AssignmentTracker branch)
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
        internal void Combine(IEnumerable<AssignmentTracker> branches)
        {
            HashSet<VariableDeclaration> set = null;
            // first merge the branches
            foreach (AssignmentTracker tracker in branches)
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

            VariableDeclaration decl = lhs.TryGetTargetEntity<VariableDeclaration>(out NameReference dummy);
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