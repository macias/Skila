using System.Collections.Generic;
using System.Linq;

namespace Skila.Language
{
    public sealed class TemplateParametersBuffer
    {
        private readonly List<TemplateParameter> values;
        public IEnumerable<TemplateParameter> Values => this.values;

        private TemplateParametersBuffer()
        {
            this.values = new List<TemplateParameter>();
        }
        public static TemplateParametersBuffer Create()
        {
            return new TemplateParametersBuffer();
        }
        public TemplateParametersBuffer Add(string name, VarianceMode mode)
        {
            return Add(name, mode, EntityModifier.None, Enumerable.Empty<NameReference>(), Enumerable.Empty<NameReference>());
        }
        public TemplateParametersBuffer Add(string name, VarianceMode mode, EntityModifier constraintModifier,
            IEnumerable<NameReference> inherits, IEnumerable<NameReference> baseOf)
        {
            this.values.Add(new TemplateParameter(this.values.Count, name, mode, constraintModifier, inherits, baseOf));
            return this;
        }
    }
}
