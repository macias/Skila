using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    //[DebuggerDisplay("{GetType().Name} {ToString()}")]
  /*@@@  public sealed class EnumCaseBuilder : IMultiBuilder<VariableDeclaration>
    {
        public static EnumCaseBuilder Create(params string[] cases)
        {
            return new EnumCaseBuilder(cases);
        }

        private readonly IEnumerable<string> cases;

        public EnumCaseBuilder(string[] cases)
        {
            this.cases = cases;
        }

        public IEnumerable<VariableDeclaration> Build()
        {
            if (build == null)
                build = new TemplateConstraint(name, modifier, hasConstraints, inherits, baseOf);
            return build;
        }
    }*/
}
