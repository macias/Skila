using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NaiveLanguageTools.Common;
using Skila.Language.Builders;
using Skila.Language.Comparers;
using Skila.Language.Extensions;
using System.Runtime.CompilerServices;
using Skila.Language.Semantics;

namespace Skila.Language.Entities
{
    [DebuggerDisplay("{GetType().Name} {ToString()}")]
    public abstract class TemplateDefinition : Node, IEntity, IEntityScope
    {
        private readonly HashSet<INode> ownedNodes;
        private readonly Lazy<EntityInstance> instanceOf;
        public EntityInstance InstanceOf => this.instanceOf.Value;

        public NameDefinition Name { get; }

        public IEnumerable<TypeDefinition> NestedTypes => this.NestedTemplates.WhereType<TypeDefinition>();
        public IEnumerable<FunctionDefinition> NestedFunctions => this.NestedTemplates.WhereType<FunctionDefinition>();
        // directly nested fields + property fields
        public IEnumerable<VariableDefiniton> AllNestedFields => this.ownedNodes.WhereType<VariableDefiniton>()
            .Concat(this.NestedProperties.SelectMany(it => it.Fields));
        public IEnumerable<Property> NestedProperties => this.ownedNodes.WhereType<Property>();
        public IEnumerable<Namespace> NestedNamespaces => this.NestedTemplates.WhereType<Namespace>();
        public IEnumerable<TypeContainerDefinition> NestedTypeContainers => this.NestedTemplates.WhereType<TypeContainerDefinition>();
        public IEnumerable<TemplateDefinition> NestedTemplates => this.ownedNodes.WhereType<TemplateDefinition>();
        public IEnumerable<IEntity> NestedEntities => this.ownedNodes.WhereType<IEntity>();

        public override IEnumerable<INode> OwnedNodes => this.ownedNodes.Concat(this.Name)
            .Concat(Conditionals)
            .Where(it => it!=null);

        // every template will hold each created instance of it, so for example List<T> can hold List<string>, List<int> and so on
        // the purpose -- to have just single instance per template+arguments
        private readonly Dictionary<IReadOnlyCollection<IEntityInstance>, EntityInstance> instancesCache;

        public bool IsComputed { get; protected set; }
        public IEntityInstance Evaluation { get; protected set; }
        public ValidationData Validation { get; set; }

        public EntityModifier Modifier { get; protected set; }
        public IEnumerable< TemplateConstraint> Constraints { get; }
        // constraints that sets the availability of the entire template
        // for example "Array<T>" can have method "copy" present with conditional on the method (not entire type)
        // that T has method "copy" itself, otherwise the method "Array.copy" is not available
        public IEnumerable<TemplateConstraint> Conditionals { get; }

        // used to protect ourselves against adding extra nodes after the object is built
        // used in functions and types, not namespaces
        protected bool constructionCompleted;

        protected TemplateDefinition(EntityModifier modifier, NameDefinition name,
            IEnumerable<TemplateConstraint> constraints) : base()
        {
            this.Modifier = modifier;
            this.Constraints = (constraints??Enumerable.Empty<TemplateConstraint>()).StoreReadOnly();
            this.ownedNodes = new HashSet<INode>(ReferenceEqualityComparer<INode>.Instance);
            this.Name = name;
            this.instancesCache = new Dictionary<IReadOnlyCollection<IEntityInstance>, EntityInstance>(EqualityComparer.Create<IReadOnlyCollection<IEntityInstance>>(
                (aa, bb) =>
                {
                    // please note that for functions we need to cache fully specified names and bare as well, consider
                    // call<T>(i);
                    // and
                    // call(i); // type parameters are inferred
                    if (aa.Count != bb.Count)
                        return false;

                    foreach (var ab in aa.Zip(bb, (a, b) => Tuple.Create(a, b)))
                        if (!ab.Item1.IsSame(ab.Item2, jokerMatchesAll: false))
                            return false;
                    return true;
                }, aa => aa.Count.GetHashCode() ^ aa.Aggregate(0, (acc, a) => acc ^ RuntimeHelpers.GetHashCode(a))));

            {
                var set = this.Constraints.ToHashSet();
                foreach (TemplateParameter param in name.Parameters)
                {
                    TemplateConstraint constraint = this.Constraints.SingleOrDefault(it => it.Name.Name == param.Name);
                    param.SetConstraint(constraint);
                    set.Remove(constraint);

                    this.AddNode(param.AssociatedType);
                }

                this.Conditionals = set;
            }

            this.instanceOf = new Lazy<EntityInstance>(() => this.GetInstanceOf(this.Name.Parameters.Select(it => it.InstanceOf)));
        }

        public T AddBuilder<T>(IBuilder<T> builder)
            where T : INode
        {
            return this.AddNode(builder.Build());
        }
        public T AddNode<T>(T elem)
            // todo: set constraint as IEntity and move towards meaning "members"
            where T : INode
        {
            if (this.constructionCompleted)
                throw new Exception("Internal error");

            if (!ownedNodes.Add(elem))
                throw new ArgumentException("Element was already added.");
            elem.AttachTo(this);
            return elem;
        }

        public bool ContainsElement(TemplateDefinition elem)
        {
            return this.ownedNodes.Contains(elem);
        }

        public EntityInstance GetInstanceOf(IEnumerable<IEntityInstance> arguments)
        {
            IReadOnlyCollection<IEntityInstance> args = (arguments ?? Enumerable.Empty<IEntityInstance>()).StoreReadOnly();

            EntityInstance result;
            if (!this.instancesCache.TryGetValue(args, out result))
            {
                result = EntityInstance.RAW_CreateUnregistered(this, args);
                this.instancesCache.Add(args, result);
            }
            return result;
        }

        public override string ToString()
        {
            return this.Name.ToString();
        }

        public virtual void Validate( ComputationContext ctx)
        {
            ;
        }
        public abstract void Evaluate(ComputationContext ctx);

    }
}