using Skila.Language.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Language.Semantics
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class AssignmentTracker : INameRegistry
    {
#if DEBUG
        public DebugId DebugId { get; } = new DebugId(typeof(AssignmentTracker));
#endif

        // this one is for telling if we can read (from initialized? variable)
        private readonly HashSet<VariableDeclaration> initializedVariables;
        
        // this one is for telling if we can initialize via assigment local variable
        private readonly HashSet<VariableDeclaration> assignedVariables;
        // this one is for telling if assigment is effectively duplicated because it is placed within loop
        private readonly Dictionary<VariableDeclaration,int> variableLoopLevels;

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
            this.initializedVariables = new HashSet<VariableDeclaration>(comparer: variableDeclarationComparer);
            this.assignedVariables = new HashSet<VariableDeclaration>(comparer: variableDeclarationComparer);
            this.variableLoopLevels = new Dictionary<VariableDeclaration, int>(comparer: variableDeclarationComparer);
        }

        private AssignmentTracker(AssignmentTracker src)
        {
            this.initializedVariables = new HashSet<VariableDeclaration>(src.initializedVariables, comparer: variableDeclarationComparer);
            this.assignedVariables = new HashSet<VariableDeclaration>(src.assignedVariables, comparer: variableDeclarationComparer);
            // this info is fixed per entire function so we can share it
            this.variableLoopLevels = src.variableLoopLevels;
        }

        public AssignmentTracker Clone()
        {
            return new AssignmentTracker(this);
        }

        internal void Add(VariableDeclaration decl,int loopLevel)
        {
            this.variableLoopLevels.Add(decl, loopLevel);
            if (decl.InitValue != null)
            {
                this.initializedVariables.Add(decl);
                this.assignedVariables.Add(decl); 
            }
        }

        internal void MergeInitializations()
        {
            HashSet<VariableDeclaration> set = ThenBranch?.initializedVariables;
            // first merge the branches
            if (ElseBranch?.initializedVariables != null)
            {
                if (set == null)
                    set = ElseBranch.initializedVariables;
                else
                {
                    set = new HashSet<VariableDeclaration>(set);
                    set.IntersectWith(ElseBranch.initializedVariables);
                }
            }

            // ... then merge the outcome with current tracker
            if (set != null)
                this.initializedVariables.UnionWith(set);
        }

        internal void MergeAssigments()
        {
            // this merge is simpler, we just need to know if ever given variable was assigned
            // because for read-only ones it can be done only once
            if (ThenBranch != null)
                this.assignedVariables.UnionWith(ThenBranch.assignedVariables);
            if (ElseBranch != null)
                this.assignedVariables.UnionWith(ElseBranch.assignedVariables);
        }
        public void ResetBranches()
        {
            this.ThenBranch = null;
            this.ElseBranch = null;
        }

        internal bool AssignedLocal(IExpression lhs,int loopLevel)
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

            VariableDeclaration decl = lhs.TryGetTargetEntity<VariableDeclaration>(out NameReference name_ref);
            if (decl == null || !name_ref.Binding.Match.IsLocal) 
                return true;

            this.initializedVariables.Add(decl);
            bool first_assign = this.assignedVariables.Add(decl);
            if (decl.Modifier.HasReassignable)
                return true;

            // in case of readonly variables we can assign it only once AND not in loop (because it would be doubled assigment effectively)
            return first_assign && this.variableLoopLevels[decl] == loopLevel;
        }

        public bool TryCanRead(NameReference name, out VariableDeclaration varDeclaration)
        {
            if (name.Binding.Match.Instance.Target is VariableDeclaration decl)
            {
                varDeclaration = decl;

                if (this.initializedVariables.Contains(decl))
                {
                    return true;
                }
                else if (name.Binding.Match.IsLocal)
                    return false;
            }
            else
                varDeclaration = null;

            return true;
        }

        public override string ToString()
        {
            if (this.initializedVariables.Any())
                return this.initializedVariables.Select(it => $"{it.Name}").Join(", ");
            else
                return "<empty>";
        }

    }

}