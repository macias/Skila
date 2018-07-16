using Skila.Language.Extensions;

namespace Skila.Language
{
    public sealed class TimedIEntityInstance : ITimedIEntityInstance
    {
        public static TimedIEntityInstance Create(Lifetime lifetime, IEntityInstance instance)
        {
            return new TimedIEntityInstance(lifetime, instance);
        }

        public Lifetime Lifetime { get; private set; }
        public IEntityInstance Instance { get; }

        private TimedIEntityInstance( Lifetime lifetime, IEntityInstance instance)
        {
            this.Lifetime = lifetime;
            this.Instance = instance;
        }

        public override string ToString()
        {
            return $"{this.Instance}@{this.Lifetime}";
        }

        public void SetLifetime(ComputationContext ctx, Lifetime lifetime)
        {
            this.Lifetime = lifetime;
            this.Instance.Rebuild(ctx, lifetime, deep: false);
        }
    }
}
