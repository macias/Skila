namespace Skila.Language
{
    public struct TypeMatching
    {
        public static TypeMatching Create(bool allowSlicing, bool literalSource = false)
        {
            return new TypeMatching()
            {
                AllowSlicing = allowSlicing,
                LiteralSource = literalSource,
                Position = VarianceMode.Out
            };
        }

        public bool AllowSlicing { get; set; }
        public bool LiteralSource { get; set; }
        public VarianceMode Position { get; set; }

        internal TypeMatching EnabledSlicing()
        {
            TypeMatching result = this;
            result.AllowSlicing = true;
            return result;
        }
    }
}
