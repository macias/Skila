namespace Skila.Language
{
    public interface ITimedIEntityInstance
    {
        IEntityInstance Instance { get; }
        Lifetime Lifetime { get; }

        void SetLifetime(ComputationContext ctx, Lifetime lifetime);
    }
}