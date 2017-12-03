using System;
using System.Collections.Generic;
using System.Linq;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public struct ExecutionFlow
    {
        public static ExecutionFlow Empty { get; } = new ExecutionFlow(null, null);

        internal static ExecutionFlow CreateFork(IExpression condition, IExpression thenBody, IExpression elseBranch)
        {
            var maybes = new List<IEnumerable<IExpression>>(); 
            if (thenBody != null)
                maybes.Add(new[] { thenBody });
            if (elseBranch!=null)
                maybes.Add(new[] { elseBranch });
            return new ExecutionFlow(new[] { condition }, null, maybes.ToArray());
        }
        internal static ExecutionFlow CreateElse(IExpression elseBody, IExpression nextBranch)
        {
            // from else POV the body will be always executed, thus we put it as "always" path
            // in correct code "next" does not exist, but user could write incorrect code and we have to put it somewhere
            // again from else POV such branch is alternative, thus we put it as "maybe"
            if (nextBranch == null)
                return new ExecutionFlow(always: new[] { elseBody }, postMaybes: null);
            else
                return new ExecutionFlow(always: new[] { elseBody }, postMaybes: null, maybes: new[] { nextBranch });
        }
        internal static ExecutionFlow CreateLoop(IEnumerable<IExpression> always,
            IEnumerable<IExpression> maybePath, IEnumerable<IExpression> postMaybes)
        {
            maybePath = maybePath.Where(it => it != null).StoreReadOnly();
            postMaybes = postMaybes.Where(it => it != null).StoreReadOnly();
            always = always.Where(it => it != null).StoreReadOnly();

            return new ExecutionFlow(always, postMaybes, maybePath);
        }
        public static ExecutionFlow CreatePath(IEnumerable<IExpression> path)
        {
            return new ExecutionFlow(path, null);
        }
        public static ExecutionFlow CreatePath(params IExpression[] path)
        {
            return new ExecutionFlow(path, null);
        }

        public IEnumerable<IExpression> Enumerate => AlwaysPath.Concat(MaybePaths.Flatten()).Concat(PostMaybes);


        public bool ExhaustiveMaybes { get; }
        public IEnumerable<IExpression> AlwaysPath { get; }
        // this is fork, not a sequence
        public IReadOnlyCollection<IEnumerable<IExpression>> MaybePaths { get; }
        // executed on loop-continue, but not break, post maybe follows first maybe path
        public IEnumerable<IExpression> PostMaybes { get; }

        // we make always path exceptional to accomodate FunctionParameter type
        public ExecutionFlow(IEnumerable<IExpression> always, IEnumerable<IExpression> postMaybes,
            params IEnumerable<IExpression>[] maybes)
        {
            this.AlwaysPath = (always?.Where(it => it != null) ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            this.MaybePaths = (maybes ?? Enumerable.Empty<IEnumerable<IExpression>>()).StoreReadOnly();
            this.PostMaybes = (postMaybes ?? Enumerable.Empty<IExpression>()).StoreReadOnly();
            if (this.MaybePaths.Count > 2)
                throw new ArgumentException("We can have only two maybes max");
            this.ExhaustiveMaybes = this.MaybePaths.Count != 1;
        }

    }

}
