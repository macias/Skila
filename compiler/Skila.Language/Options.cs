﻿using NaiveLanguageTools.Common;
using System;
using System.Linq;

namespace Skila.Language
{
    public sealed class Options : IOptions
    {
        private bool debugThrowOnError;
        public bool DebugThrowOnError
        {
            get
            {
                return debugThrowOnError;
                //return false;
            }
            set { debugThrowOnError = value; }
        }

        public bool RelaxedMode { get; set; }
        private bool allowEmptyFieldTypeNames;
        public bool AllowEmptyFieldTypeNames
        {
            get { return this.allowEmptyFieldTypeNames || this.RelaxedMode; }
            set { this.allowEmptyFieldTypeNames = value; }
        }
        private bool allowNamedSelf;
        public bool AllowNamedSelf
        {
            get { return this.allowNamedSelf || this.RelaxedMode; }
            set { this.allowNamedSelf = value; }
        }


        public bool StaticMemberOnlyThroughTypeName { get; set; }
        public bool InterfaceDuckTyping { get; set; }
        public bool ScopeShadowing { get; set; }
        public bool ReferencingBase { get; set; }
        public bool DiscardingAnyExpressionDuringTests { get; set; }
        public bool GlobalVariables { get; set; }
        public bool MiniEnvironment { get; set; }
        public bool AllowInvalidMainResult { get; set; }
        public bool AllowProtocols { get; set; }
        public bool AllowRealMagic { get; set; }
        public bool AtomicPrimitivesMutable { get; set; }
        private bool? singleMutability;
        public bool SingleMutability
        {
            get
            {
                if (!this.singleMutability.HasValue)
                    throw new Exception();

                return this.singleMutability.Value;
            }
        }

        public Options()
        {

        }

        public Options SetSingleMutability(bool value)
        {
            if (this.singleMutability.HasValue)
                throw new Exception();

            this.singleMutability = value;
            return this;
        }
        public Options EnableSingleMutability()
        {
            if (this.singleMutability.HasValue)
                throw new Exception();

            this.singleMutability = true;
            return this;
        }
        public Options DisableSingleMutability()
        {
            if (this.singleMutability.HasValue)
                throw new Exception();

            this.singleMutability = false;
            return this;
        }
        public override string ToString()
        {
            return this.GetEnabledProperties().Join(", ");
        }
    }
}
