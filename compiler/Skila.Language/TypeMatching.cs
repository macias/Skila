namespace Skila.Language
{
    public struct TypeMatching
    {
        public static TypeMatching Create(bool allowSlicing)
        {
            return new TypeMatching()
            {
                AllowSlicing = allowSlicing,
                Position = VarianceMode.Out
            };
        }

        public bool AllowSlicing { get; set; }
        public VarianceMode Position { get; set; }
        public bool IgnoreMutability { get; set; }

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
