using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Skila.Language.Entities;
using Skila.Language.Expressions;

namespace Skila.Language.Builders
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EnumCaseBuilder 
    {
        public static EnumCaseBuilder Create(params string[] cases)
        {
            return new EnumCaseBuilder(cases);
        }

        private readonly IEnumerable<string> cases;
        private List<VariableDeclaration> build;

        private EnumCaseBuilder(string[] cases)
        {
            this.cases = cases;
        }

        public IEnumerable<VariableDeclaration> Build(TypeBuilder typeBuilder)
        {
            if (build == null)
            {
                build = new List<VariableDeclaration>();
                NameReference typename = typeBuilder.CreateTypeNameReference();
                foreach (string s in cases)
                {
                    build.Add(VariableDeclaration.CreateStatement(s, typename,
                        // we cannot set the initial value here because we don't know how many cases are in parent enums
                        Undef.Create(),
                        EntityModifier.Enum | EntityModifier.Static | EntityModifier.Public));
                }
            }

            return build;
        }
    }
}
