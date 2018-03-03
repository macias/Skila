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

        private readonly HashSet<VariableDeclaration> initializedVariables;

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
        }

        private AssignmentTracker(AssignmentTracker src)
        {
            this.initializedVariables = new HashSet<VariableDeclaration>(src.initializedVariables, comparer: variableDeclarationComparer);

        }

        public AssignmentTracker Clone()
        {
            return new AssignmentTracker(this);
        }

        internal void Add(VariableDeclaration decl)
        {
            if (decl.InitValue != null)
                this.initializedVariables.Add(decl);
        }

        internal void MergeAssignments()
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

        public void ResetBranches()
        {
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
                this.initializedVariables.Add(decl);
            }
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

        public bool CanRead(VariableDeclaration decl)
        {
            return decl.InitValue != null;
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