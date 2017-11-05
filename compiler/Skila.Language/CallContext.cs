using Skila.Language.Expressions;

namespace Skila.Language
{
    internal struct CallContext
    {
        public IEntityInstance StaticContext { get; set; }
        // can be null non-methods, or for static ones
        public FunctionArgument MetaThisArgument { get; set; }

        public IEntityInstance Evaluation => this.MetaThisArgument?.Evaluation ?? this.StaticContext;
    }
}
