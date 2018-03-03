using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Language.Semantics
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed partial class AssignmentTracker : INameRegistry
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(AssignmentTracker));
#endif

        private readonly IDictionary<VariableDeclaration, VariableState> variables;

        private static IEqualityComparer<VariableDeclaration> variableDeclarationComparer;
        public AssignmentTracker ThenBranch { get; set; }
        public AssignmentTracker ElseBranch { get; set; }

        static AssignmentTracker()
        {
            variableDeclarationComparer = ReferenceEqualityComparer<VariableDeclaration>.Instance;
        }
        public AssignmentTracker()
        {
            // binding is done during evaluation, so now we can ignore shadowing option and use references in the dictionary
            this.variables = new Dictionary<VariableDeclaration, VariableState>(comparer: variableDeclarationComparer);
            if (this.DebugId.Id == 1669)
            {
                ;
            }
        }

        private AssignmentTracker(AssignmentTracker src)
        {
            this.variables = new Dictionary<VariableDeclaration, VariableState>(src.variables, comparer: variableDeclarationComparer);

            if (this.DebugId.Id == 1669)
            {
                ;
            }
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
            this.variables.Add(decl, decl.InitValue != null ? VariableState.Assigned : VariableState.NotInitialized);
        }

        internal void ImportVariables()
        {
            foreach (VariableDeclaration decl in new[] { ThenBranch, ElseBranch }
                .Where(it => it != null)
                .SelectMany(it => it.variables.Keys))
            {
                if (!this.variables.ContainsKey(decl))
                    this.variables.Add(decl, VariableState.NotInitialized);
            }
        }

        internal void MergeAssignments(params AssignmentTracker[] branches)
        {
            HashSet<VariableDeclaration> set = null;
            // first merge the branches
            foreach (AssignmentTracker branch in new[] { ThenBranch, ElseBranch }.Where(it => it != null))
            {
                IEnumerable<VariableDeclaration> locally_set = branch.variables
                .Where(it => it.Value == VariableState.Assigned)
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
                    if (this.variables.ContainsKey(decl))
                        this.variables[decl] = VariableState.Assigned;
                }

            // since we merge both branches now it is time to kill them because we will represent state of variables as merges ones
            this.ThenBranch = null;
            this.ElseBranch = null;
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

            VariableDeclaration decl = lhs.TryGetTargetEntity<VariableDeclaration>(out NameReference dummy);
            if (decl != null)
            {
                if (this.variables.ContainsKey(decl))
                    this.variables[decl] = VariableState.Assigned;
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

                if (this.variables.TryGetValue(decl, out VariableState state))
                {
                    return state != VariableState.NotInitialized;
                }
            }
            else
                varDeclaration = null;

            return true;
        }

        public bool CanRead(VariableDeclaration decl)
        {
            VariableState state = this.variables[decl];
            return state != VariableState.NotInitialized;
        }

        public override string ToString()
        {
            return $"{variables.Count} entries, {variables.Count(it => it.Value != VariableState.NotInitialized)} assigned";
        }

    }

}