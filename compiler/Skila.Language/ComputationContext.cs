using System;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using Skila.Language.Semantics;

namespace Skila.Language
{
    public struct ComputationContext
    {
        public static ComputationContext Create(Environment env)
        {
            return new ComputationContext(env, bare: false);
        }

        public static ComputationContext CreateBare(Environment env)
        {
            return new ComputationContext(env,bare:true);
        }

        internal AutoName AutoName { get; }
        public Environment Env { get; }
        internal NameRegistry EvalLocalNames;
        public AssignmentTracker ValAssignTracker;

        public ErrorManager ErrorManager { get; }

        private readonly HashSet<INode> visited;

        private ComputationContext(Environment env,bool bare) : this()
        {
            this.Env = env;
            if (!bare)
            {
                this.ErrorManager = ErrorManager.Create();
                this.visited = new HashSet<INode>(ReferenceEqualityComparer<INode>.Instance);
                this.AutoName = new AutoName();
            }
        }

        internal void AddError(ErrorCode code, INode node, INode context = null)
        {
            this.ErrorManager.AddError(code, node, context);
        }

        internal bool AddVisited(INode node)
        {
            return this.visited.Add(node);
        }

        internal void RemoveVisited(INode node)
        {
            if (!this.visited.Remove(node))
                throw new InvalidOperationException();
        }
    }
}
