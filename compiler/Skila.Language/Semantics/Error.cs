using NaiveLanguageTools.Common;
using Skila.Language.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Skila.Language.Semantics
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class Error
    {
        public static Error Create(ErrorCode code, INode node,IEnumerable< INode> context)
        {
            return new Error(code,node,context);
        }
        public ErrorCode Code { get; }
        public INode Node { get; }
        private readonly HashSet<INode> context;
        public IEnumerable<INode> Context => this.context;

        private Error(ErrorCode code,INode node, IEnumerable<INode> context)
        {
            if (node == null)
                throw new ArgumentNullException();

            this.Code = code;
            this.Node = node;
            this.context = context.ToHashSet(ReferenceEqualityComparer<INode>.Instance);
        }

        public bool SameNodeInvolved(Error other)
        {
            return this.Node == other.Node 
                || other.context.Contains(this.Node) 
                || this.context.Contains(other.Node)
                || this.context.Overlaps(other.Context);
        }

        public override string ToString()
        {
            return this.Code.ToString() + " " + this.Node.ToString()+(this.Context.Any()?$" related to {String.Join(",",Context)}":"");
        }
    }

}
