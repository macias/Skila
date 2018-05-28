namespace Skila.Language
{
    public enum ConstraintMatch
    {
        Yes,
        MutabilityViolation,
        BaseViolation,
        InheritsViolation,
        MissingFunction,
        UndefinedTemplateArguments,
        AssignabilityViolation,
    }
}
