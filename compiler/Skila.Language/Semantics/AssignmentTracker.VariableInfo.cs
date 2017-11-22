namespace Skila.Language.Semantics
{
    public sealed partial class AssignmentTracker
    {
        private sealed class VariableInfo
        {
            public VariableState State { get; private set; }
            public int DeclarationId { get; }
            public int AssignmentId { get; private set; }
            
            // tracking whether variable is read is handy, but it is not core feature of this type
            public bool IsRead { get; internal set; }
            public bool IsCloned { get; }

            internal VariableInfo(VariableState state, int declId) 
            {
                this.State = state;
                this.DeclarationId = declId;
                this.AssignmentId = declId;
            }

            internal VariableInfo(VariableInfo src)
            {
                this.State = src.State;
                this.DeclarationId = src.DeclarationId;
                this.AssignmentId = src.AssignmentId;
                this.IsRead = src.IsRead;
                this.IsCloned = true;
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
        }
    }

}