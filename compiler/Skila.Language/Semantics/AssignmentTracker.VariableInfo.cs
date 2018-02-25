namespace Skila.Language.Semantics
{
    public sealed partial class AssignmentTracker
    {
        private sealed class VariableInfo
        {
#if DEBUG
            public DebugId DebugId { get; }
#endif

            public VariableState State { get; private set; }
            public int DeclarationId { get; }
            public int AssignmentId { get; private set; }

            // tracking whether variable was read is handy, but it is not core feature of this type
            // it is about tracking if we used the variable before it was initialized
            public bool IsRead { get; internal set; }

            private VariableInfo(VariableState state, int declId, int assignId, bool isRead)
            {
#if DEBUG
                this.DebugId = new DebugId(this.GetType());
#endif
                this.State = state;
                this.DeclarationId = declId;
                this.AssignmentId = assignId;
                this.IsRead = isRead;

                if (this.DebugId.Id == 167)
                {
                    ;
                }
            }

            internal VariableInfo(VariableState state, int declId)
                : this(state, declId, declId, isRead: false)
            {
            }

            private VariableInfo(VariableInfo src)
                : this(src.State, src.DeclarationId, src.AssignmentId, src.IsRead)
            {
            }

            internal void Assign(VariableState state, int assignId)
            {
                this.State = state;
                this.AssignmentId = assignId;
            }

            internal VariableInfo Clone()
            {
                return new VariableInfo(this);
            }

            public override string ToString()
            {
                return $"{State} {IsRead} {DeclarationId}/{AssignmentId}";
            }
        }
    }

}