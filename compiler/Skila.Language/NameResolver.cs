using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;
using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class NameResolver : IErrorReporter
    {
        public static NameResolver Create(Environment env)
        {
            return new NameResolver(env);
        }

        public ComputationContext Context { get; }

        public ErrorManager ErrorManager => this.Context.ErrorManager;
        public IEnumerable<Error> Errors => this.ErrorManager.Errors;

        private NameResolver(Environment env)
        {
            this.Context = ComputationContext.Create(env);

            env.Root.Surfed(this.Context);

            env.Root.Evaluated(this.Context, EvaluationCall.Nested);

            env.Root.Validated(this.Context);
        }

    }
}
