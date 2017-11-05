using System;
using System.Collections.Generic;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Semantics;

namespace Skila.Language
{
    public sealed class Options : IOptions
    {
        public bool StaticMemberOnlyThroughTypeName { get; set; }
    }
}
