﻿using System;
using System.Collections.Generic;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Presentation.Quantum.ViewModels;
using SiliconStudio.Quantum;

namespace SiliconStudio.Presentation.Quantum.Presenters
{
    public class VirtualNodePresenter : NodePresenterBase
    {
        protected NodeAccessor AssociatedNode;
        [NotNull] private readonly Func<object> getter;
        private readonly Action<object> setter;
        private readonly List<Attribute> memberAttributes = new List<Attribute>();
        private bool updatingValue;

        public VirtualNodePresenter([NotNull] INodePresenterFactoryInternal factory, IPropertyProviderViewModel propertyProvider, [NotNull] INodePresenter parent, string name, Type type, int? order, [NotNull] Func<object> getter, Action<object> setter)
            : base(factory, propertyProvider, parent)
        {
            this.getter = getter;
            this.setter = setter;
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (getter == null) throw new ArgumentNullException(nameof(getter));
            Name = name;
            CombineKey = Name;
            DisplayName = Name;
            Type = type;
            Order = order;
            Descriptor = TypeDescriptorFactory.Default.Find(type);

            AttachCommands();
        }

        public sealed override List<INodePresenterCommand> Commands { get; } = new List<INodePresenterCommand>();

        public override Type Type { get; }

        public override bool IsEnumerable => false;

        public override Index Index => AssociatedNode.Node != null ? AssociatedNode.Index : Index.Empty;

        public override ITypeDescriptor Descriptor { get; }

        public override object Value => getter();

        public IReadOnlyList<Attribute> MemberAttributes => memberAttributes;

        protected override IObjectNode ParentingNode => null;

        /// <summary>
        /// Registers an <see cref="IGraphNode"/> object to this virtual node so when the node vakye is modified, it will raise the
        /// <see cref="NodePresenterBase.ValueChanging"/> and <see cref="NodePresenterBase.ValueChanged"/> events.
        /// </summary>
        /// <param name="associatedNodeAccessor">An accessor to the node to register.</param>
        /// <remarks>Events subscriptions are cleaned when this virtual node is disposed.</remarks>
        public virtual void RegisterAssociatedNode(NodeAccessor associatedNodeAccessor)
        {
            if (AssociatedNode.Node != null)
                throw new InvalidOperationException("A content has already been registered to this virtual node");

            AssociatedNode = associatedNodeAccessor;
            AssociatedNode.Node.RegisterChanging(AssociatedNodeChanging);
            AssociatedNode.Node.RegisterChanged(AssociatedNodeChanged);
        }

        public override void Dispose()
        {
            if (AssociatedNode.Node != null)
            {
                AssociatedNode.Node.UnregisterChanging(AssociatedNodeChanging);
                AssociatedNode.Node.UnregisterChanged(AssociatedNodeChanged);
            }
            base.Dispose();
        }

        public override void UpdateValue(object newValue)
        {
            try
            {
                var oldValue = getter();
                var changeType = Index == Index.Empty ? ContentChangeType.ValueChange : ContentChangeType.CollectionUpdate;
                RaiseNodeChanging(newValue, changeType, Index);
                updatingValue = true;
                setter(newValue);
                updatingValue = false;
                RaiseNodeChanged(oldValue, changeType, Index);
            }
            catch (Exception e)
            {
                throw new NodePresenterException("An error occurred while updating the value of the node, see the inner exception for more information.", e);
            }
            finally
            {
                // Note: not sure it is worth doing this in finally block. Currently if we have an exception here we're already screwed.
                updatingValue = false;
            }
        }

        public override void AddItem(object value)
        {
            throw new NodePresenterException($"{nameof(AddItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        public override void AddItem(object value, Index index)
        {
            throw new NodePresenterException($"{nameof(AddItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        public override void RemoveItem(object value, Index index)
        {
            throw new NodePresenterException($"{nameof(RemoveItem)} cannot be used on a {nameof(VirtualNodePresenter)}.");
        }

        public override NodeAccessor GetNodeAccessor()
        {
            return default(NodeAccessor);
        }

        private void AssociatedNodeChanging(object sender, INodeChangeEventArgs e)
        {
            RaiseNodeChanging(e.NewValue, e.ChangeType, e.Index);
        }

        private void AssociatedNodeChanged(object sender, INodeChangeEventArgs e)
        {
            RaiseNodeChanged(e.OldValue, e.ChangeType, e.Index);
        }

        private void RaiseNodeChanging(object newValue, ContentChangeType changeType, Index index)
        {
            if (ShouldRaiseEvent(changeType, index))
            {
                RaiseValueChanging(newValue);
            }
        }

        private void RaiseNodeChanged(object oldValue, ContentChangeType changeType, Index index)
        {
            if (ShouldRaiseEvent(changeType, index))
            {
                RaiseValueChanged(Value);
            }
        }

        private bool ShouldRaiseEvent(ContentChangeType changeType, Index index)
        {
            if (updatingValue)
                return false;

            if (AssociatedNode.Node == null || AssociatedNode.Index == Index.Empty)
                return true;

            return index != Index.Empty && ItemNodePresenter.IsValidChange(changeType, index, Index);
        }
    }
}