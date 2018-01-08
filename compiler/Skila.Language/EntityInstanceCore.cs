using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Entities;
using Skila.Language.Extensions;

namespace Skila.Language
{
    // instance of an entity, so it could be variable or closed type like "Tuple<Int,Int>"
    // but not open type like "Array<T>" (it is an entity, not an instance of it)

    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public sealed class EntityInstanceCore 
    {
        public static EntityInstanceCore RAW_CreateUnregistered(IEntity target, IEnumerable<IEntityInstance> arguments,
            MutabilityFlag overrideMutability)
        {
            return new EntityInstanceCore(target, arguments, overrideMutability);
        }

#if DEBUG
        public DebugId DebugId { get; } = new DebugId();
#endif

        public bool IsJoker => this.Target == TypeDefinition.Joker;

        // currently modifier only applies to types mutable/immutable and works as notification
        // that despite the type is immutable we would like to treat is as mutable
        public MutabilityFlag OverrideMutability { get; } // we use bool flag instead full EntityModifer because so far we don't have other modifiers

        public IEntity Target { get; }
        public TypeDefinition TargetType => this.Target.CastType();
        public TemplateDefinition TargetTemplate => this.Target.Cast<TemplateDefinition>();
        public IReadOnlyList<IEntityInstance> TemplateArguments { get; }
        public bool TargetsTemplateParameter => this.Target.IsType() && this.TargetType.IsTemplateParameter;
        public TemplateParameter TemplateParameterTarget => this.TargetType.TemplateParameter;
        public bool MissingTemplateArguments => !this.TemplateArguments.Any() && this.Target.Name.Arity > 0;

        public bool IsTypeImplementation => this.Target.IsType() && this.TargetType.IsTypeImplementation;

        public bool DependsOnTypeParameter_UNUSED
        {
            get
            {
                return this.TargetsTemplateParameter || this.TemplateArguments.Any(it => it.DependsOnTypeParameter);
            }
        }

        private EntityInstanceCore(IEntity target, IEnumerable<IEntityInstance> arguments, MutabilityFlag overrideMutability)
        {
            if (target == null)
                throw new ArgumentNullException("Instance has to be created for existing entity");

            arguments = arguments ?? Enumerable.Empty<IEntityInstance>();
            this.Target = target;
            this.TemplateArguments = arguments.StoreReadOnlyList();
            this.OverrideMutability = overrideMutability;
        }

        public override string ToString()
        {
            if (this.IsJoker)
                return "<<*>>";
            else
            {
                string args = "";
                if (this.TemplateArguments.Any())
                    args = "<" + this.TemplateArguments.Select(it => it.ToString()).Join(",") + ">";
                string mut = this.OverrideMutability == MutabilityFlag.ForceMutable ? "mutable " : this.OverrideMutability == MutabilityFlag.Neutral ? "neutral " : "";
                string result = $"{mut}{this.Target.Name.Name}{args}";
                return result;
            }
        }


    }
}