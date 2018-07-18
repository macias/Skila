namespace Skila.Language
{
    public enum LifetimeScope
    {
        Local, // regular reference
        Global, // classic pointer
        Attachment, // limited to life of its creator
    }
}