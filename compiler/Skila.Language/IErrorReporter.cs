using Skila.Language.Semantics;
using System.Collections.Generic;

namespace Skila.Language
{
    public interface IErrorReporter
    {
        IEnumerable<Error> Errors { get; }
    }
}