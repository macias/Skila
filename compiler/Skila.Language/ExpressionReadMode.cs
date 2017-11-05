namespace Skila.Language
{
    public enum ExpressionReadMode
    {
        OptionalUse, // norm in most languages
        ReadRequired,
        CannotBeRead, // type like C-void
    }
}
