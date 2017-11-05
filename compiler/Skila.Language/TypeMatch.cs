namespace Skila.Language
{
    public enum TypeMatch
    {
        No,
        Pass,
        InConversion,
        OutConversion,
        ImplicitReference,
        AutoDereference,
    }

    public static class TypeMatchExtensions
    {
        public static bool IsPerfectMatch(this TypeMatch match)
        {
            return match == TypeMatch.Pass || match == TypeMatch.AutoDereference;
        }
    }
}
