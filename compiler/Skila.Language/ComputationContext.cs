using System;
using System.Collections.Generic;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Language
{
    public struct ComputationContext
    {
        public static ComputationContext Create(Environment globals,IOptions options = null)
        {
            return new ComputationContext(globals,options);
        }

        public IOptions Options { get; }
        public Environment Env { get; }
        public NameRegistry EvalLocalNames { get; set; }
        public VariableTracker ValAssignTracker { get; set; }

        public ErrorManager ErrorManager { get; }

        private readonly HashSet<INode> visited;

        private ComputationContext(Environment globals,IOptions options) : this()
        {
            this.Options = options ?? new Options();
            this.ErrorManager = ErrorManager.Create();
            Env = globals;
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
