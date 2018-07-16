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
        public bool LifetimeCheck { get; private set; }
        public Lifetime InputLifetime { get; private set; }
        public Lifetime TargetLifetime { get; private set; }
        private bool allowLifetimeChecking;


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
        internal TypeMatching WithLifetimeCheck(bool value)
        {
            TypeMatching result = this;
            result.LifetimeCheck = value;
            return result;
        }
        internal TypeMatching WithLifetimeCheck(bool value,Lifetime inputLifetime,Lifetime targetLifetime)
        {
            if (this.allowLifetimeChecking)
            {
                TypeMatching result = this;
                result.LifetimeCheck = value;
                result.InputLifetime = inputLifetime;
                result.TargetLifetime = targetLifetime;
                return result;
            }
            else
                return this;
        }

        internal TypeMatching AllowedLifetimeChecking(bool value)
        {
            TypeMatching result = this;
            result.allowLifetimeChecking = value;
            return result;
        }
    }
}
