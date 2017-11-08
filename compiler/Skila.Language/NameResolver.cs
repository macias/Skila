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
        public static NameResolver Create(Environment env,IOptions options = null)
        {
            return new NameResolver(env,options);
        }

        public ComputationContext Context { get; }

        public ErrorManager ErrorManager => this.Context.ErrorManager;
        public IEnumerable<Error> Errors => this.ErrorManager.Errors;

        private NameResolver(Environment env,IOptions options)
        {
            this.Context = ComputationContext.Create(env,options);

            env.Root.Evaluated(this.Context);

            env.Root.Validated(this.Context);
        }

    }
}
