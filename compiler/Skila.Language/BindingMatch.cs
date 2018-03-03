namespace Skila.Language
{
    public struct BindingMatch
    {
        public static BindingMatch Joker => new BindingMatch(EntityInstance.Joker, isLocal: false);

        public EntityInstance Instance { get; }
        // used for tracking variable initializations 
        // (i.e. to check if the variable was initialized before it was read from)
        public bool IsLocal { get; }

        public BindingMatch(EntityInstance match,bool isLocal)
        {
            this.Instance = match;
            this.IsLocal = isLocal;
        }

        public override string ToString()
        {
            return (IsLocal ? "l" : "g") + "`" + Instance.ToString();
        }
    }
}