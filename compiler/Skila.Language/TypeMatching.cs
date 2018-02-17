namespace Skila.Language
{
    public struct TypeMatching
    {
        public static TypeMatching Create(bool duckTyping, bool allowSlicing)
        {
            return new TypeMatching()
            {
                DuckTyping = duckTyping,
                AllowSlicing = allowSlicing,
                Position = VarianceMode.Out
            };
        }

        public bool AllowSlicing { get; set; }
        public bool DuckTyping { get; private set; }
        public VarianceMode Position { get; set; }
        public bool IgnoreMutability { get; private set; }

        internal TypeMatching WithSlicing(bool slicing)
        {
            TypeMatching result = this;
            result.AllowSlicing = slicing;
            return result;
        }
        public TypeMatching WithIgnoredMutability(bool ignored)
        {
            TypeMatching result = this;
            result.IgnoreMutability = ignored;
            return result;
        }
    }
}
