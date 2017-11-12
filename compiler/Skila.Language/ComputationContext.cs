using System;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Language
{
    public struct ComputationContext
    {
        public static ComputationContext Create(Environment env)
        {
            return new ComputationContext(env);
        }

        public Environment Env { get; }
        public NameRegistry EvalLocalNames { get; set; }
        public VariableTracker ValAssignTracker { get; set; }

        public ErrorManager ErrorManager { get; }

        private readonly HashSet<INode> visited;

        private ComputationContext(Environment env) : this()
        {
            this.ErrorManager = ErrorManager.Create();
            Env = env;
            visited = new HashSet<INode>(ReferenceEqualityComparer<INode>.Instance);
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
