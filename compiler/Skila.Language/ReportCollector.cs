using Skila.Language.Semantics;
using System.Collections.Generic;

namespace Skila.Language
{
    public sealed class ReportCollector : IErrorReporter
    {
        private readonly List<Error> errors;
        public IEnumerable<Error> Errors => this.errors;

        public ReportCollector()
        {
            this.errors = new List<Error>();
        }

        public void Add(IErrorReporter reporter)
        {
            this.errors.AddRange(reporter.Errors);
        }
    }
}