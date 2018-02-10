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

        internal TypeMatching EnabledSlicing()
        {
            TypeMatching result = this;
            result.AllowSlicing = true;
            return result;
        }
    }
}
