using System;

namespace Skila.Language
{
    public struct TypeMatching
    {
        public static TypeMatching Create(bool duckTyping, bool allowSlicing)
        {
            return new TypeMatching()
            {
                DuckTyping = duckTyping,
                AllowSlicing = allowSlicing,
                Position = VarianceMode.Out
            };
        }

        public bool AllowSlicing { get; set; }
        public bool DuckTyping { get; private set; }
        public VarianceMode Position { get; set; }
        // this one is because of given scenario 
        public bool ForcedIgnoreMutability { get; private set; }
        // this one is check purely by data, pointers/references in general require mutability check
        // because data are shared
        public bool MutabilityCheckRequestByData { get; private set; }

        internal TypeMatching WithSlicing(bool slicing)
        {
            TypeMatching result = this;
            result.AllowSlicing = slicing;
            return result;
        }
        public TypeMatching WithIgnoredMutability(bool ignored)
        {
            TypeMatching result = this;
            result.ForcedIgnoreMutability = ignored;
            return result;
        }

        internal TypeMatching WithMutabilityCheckRequest(bool value)
        {
            // do not grant check request if we are already ignoring mutability
            if (this.ForcedIgnoreMutability)
                return this;

            TypeMatching result = this;
            result.MutabilityCheckRequestByData = value;
            return result;
        }
    }
}
