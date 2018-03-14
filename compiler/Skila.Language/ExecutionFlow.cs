using System;
using System.Collections.Generic;
using System.Linq;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public struct ExecutionFlow
    {
        public static ExecutionFlow Empty { get; } = new ExecutionFlow(null, null, null, null);

        internal static ExecutionFlow CreateFork(IExpression condition, IExpression thenBody, IExpression elseBranch)
        {
            return new ExecutionFlow(new[] { condition },
               null,
               thenMaybes: thenBody != null ? new[] { thenBody } : null,
               elseMaybes: elseBranch != null ? new[] { elseBranch } : null);
        }
        internal static ExecutionFlow CreateElse(IExpression elseBody, IExpression nextBranch)
        {
            // from else POV the body will be always executed, thus we put it as "always" path
            // in correct code "next" does not exist, but user could write incorrect code and we have to put it somewhere
            // again from else POV such branch is alternative, thus we put it as "maybe"
            if (nextBranch == null)
                return new ExecutionFlow(always: new[] { elseBody }, thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
            else
                return new ExecutionFlow(always: new[] { elseBody }, thenPostMaybes: null,
                    thenMaybes: new[] { nextBranch }, elseMaybes: null);
        }
        internal static ExecutionFlow CreateLoop(IExpression always,
            IEnumerable<IExpression> thenPath, IEnumerable<IExpression> postMaybes)
        {
            thenPath = thenPath.Where(it => it != null).StoreReadOnly();
            postMaybes = postMaybes.Where(it => it != null).StoreReadOnly();

            return new ExecutionFlow(
                new[] { always }.Where(it => it != null),
                postMaybes,
                thenMaybes: thenPath,
                elseMaybes: null);
        }
        public static ExecutionFlow CreatePath(IEnumerable<IExpression> path)
        {
            return new ExecutionFlow(path, thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
        }
        public static ExecutionFlow CreatePath(params IExpression[] path)
        {
            return new ExecutionFlow(path, thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
        }

        public IEnumerable<IExpression> Enumerate => AlwaysPath.Concat(ForkMaybePaths.Flatten())
            .Concat(ThenPostMaybes ?? Enumerable.Empty<IExpression>());


        public bool ExhaustiveMaybes => this.ThenMaybePath != null && this.ElseMaybePath != null;
        public ExecutionPath AlwaysPath { get; }

        public IEnumerable<ExecutionPath> ForkMaybePaths => new[] { ThenMaybePath, ElseMaybePath }.Where(it => it != null);
        // please note the important property of the nested maybes, when you have
        // if-then(1)-then(2)
        // in such case when you get to "then(2)", "then(1)" is executed for sure
        public ExecutionPath ThenMaybePath { get; }
        public ExecutionPath ElseMaybePath { get; }
        // executed on loop-continue, but not break, post maybe follows first maybe path
        public ExecutionPath ThenPostMaybes { get; }

        // we make always path exceptional to accomodate FunctionParameter type
        public ExecutionFlow(IEnumerable<IExpression> always, IEnumerable<IExpression> thenPostMaybes,
            IEnumerable<IExpression> thenMaybes, IEnumerable<IExpression> elseMaybes)
        {
            this.AlwaysPath = ExecutionPath.Create(always?.Where(it => it != null) ?? Enumerable.Empty<IExpression>());
            this.ThenMaybePath = ExecutionPath.Create(thenMaybes);
            this.ElseMaybePath = ExecutionPath.Create(elseMaybes);
            this.ThenPostMaybes = ExecutionPath.Create(thenPostMaybes);

            if (this.ThenPostMaybes != null && this.ElseMaybePath != null)
                throw new NotImplementedException("With else we shouldn't have post-then");
        }

    }

}
