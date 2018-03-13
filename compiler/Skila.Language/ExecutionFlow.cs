using System;
using System.Collections.Generic;
using System.Linq;
using Skila.Language.Extensions;
using NaiveLanguageTools.Common;

namespace Skila.Language
{
    public struct ExecutionFlow
    {
        public static ExecutionFlow Empty { get; } = new ExecutionFlow(createAlways(null), null, null, null);

        internal static ExecutionFlow CreateFork(IExpression condition, IExpression thenBody, IExpression elseBranch)
        {
            return new ExecutionFlow(createAlways( new[] { condition }), null,
               thenMaybes: ExecutionPath.Create( thenBody != null ? new[] { thenBody } : null),
               elseMaybes: ExecutionPath.Create(elseBranch != null ? new[] { elseBranch } : null));
        }
        internal static ExecutionFlow CreateElse(IExpression elseBody, IExpression nextBranch)
        {
            // from else POV the body will be always executed, thus we put it as "always" path
            // in correct code "next" does not exist, but user could write incorrect code and we have to put it somewhere
            // again from else POV such branch is alternative, thus we put it as "maybe"
            if (nextBranch == null)
                return new ExecutionFlow(always: createAlways( new[] { elseBody }), thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
            else
                return new ExecutionFlow(always: createAlways(new[] { elseBody }), thenPostMaybes: null, 
                    thenMaybes: ExecutionPath.Create(new[] { nextBranch }), elseMaybes: null);
        }
        internal static ExecutionFlow CreateLoop(IEnumerable<IExpression> always,
            IEnumerable<IExpression> thenPath, IEnumerable<IExpression> postMaybes)
        {
            thenPath = thenPath.Where(it => it != null).StoreReadOnly();
            postMaybes = postMaybes.Where(it => it != null).StoreReadOnly();
            always = always.Where(it => it != null).StoreReadOnly();

            return new ExecutionFlow(
                ExecutionPath.Create(always,isRepeated:true),
                ExecutionPath.Create(postMaybes, isRepeated: true), 
                thenMaybes: ExecutionPath.Create(thenPath, isRepeated: true), 
                elseMaybes: null);
        }
        public static ExecutionFlow CreatePath(IEnumerable<IExpression> path)
        {
            return new ExecutionFlow(createAlways(path), thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
        }
        public static ExecutionFlow CreatePath(params IExpression[] path)
        {
            return new ExecutionFlow(createAlways(path), thenPostMaybes: null, thenMaybes: null, elseMaybes: null);
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

        private static ExecutionPath createAlways(IEnumerable<IExpression> always)
        {
            return ExecutionPath.Create(always?.Where(it => it != null) ?? Enumerable.Empty<IExpression>());
        }
        // we make always path exceptional to accomodate FunctionParameter type
        public ExecutionFlow(ExecutionPath always, ExecutionPath thenPostMaybes,
            ExecutionPath thenMaybes, ExecutionPath elseMaybes)
        {
            this.AlwaysPath = always;;
            this.ThenMaybePath = thenMaybes;
            this.ElseMaybePath = elseMaybes;
            this.ThenPostMaybes = thenPostMaybes;

            if (this.ThenPostMaybes != null && this.ElseMaybePath != null)
                throw new NotImplementedException("With else we shouldn't have post-then");
        }

    }

}
