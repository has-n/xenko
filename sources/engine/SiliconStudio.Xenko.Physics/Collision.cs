// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Core.Threading;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Physics
{
    public class Collision
    {
        private static readonly Queue<Channel<ContactPoint>> ChannelsPool = new Queue<Channel<ContactPoint>>();

        internal Collision()
        {
        }

        public void Initialize(PhysicsComponent colliderA, PhysicsComponent colliderB)
        {
            ColliderA = colliderA;
            ColliderB = colliderB;

            NewContactChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactUpdateChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
            ContactEndedChannel = ChannelsPool.Count == 0 ? new Channel<ContactPoint> { Preference = ChannelPreference.PreferSender } : ChannelsPool.Dequeue();
        }

        internal void Destroy()
        {
            ColliderA = null;
            ColliderB = null;
            NewContactChannel.Reset();
            ContactUpdateChannel.Reset();
            ContactEndedChannel.Reset();
            ChannelsPool.Enqueue(NewContactChannel);
            ChannelsPool.Enqueue(ContactUpdateChannel);
            ChannelsPool.Enqueue(ContactEndedChannel);
            Contacts.Clear();
        }

        public PhysicsComponent ColliderA { get; private set; }

        public PhysicsComponent ColliderB { get; private set; }

        public HashSet<ContactPoint> Contacts = new HashSet<ContactPoint>(ContactPointEqualityComparer.Default);

        internal Channel<ContactPoint> NewContactChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> NewContact()
        {
            return NewContactChannel.Receive();
        }

        internal Channel<ContactPoint> ContactUpdateChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> ContactUpdate()
        {
            return ContactUpdateChannel.Receive();
        }

        internal Channel<ContactPoint> ContactEndedChannel;

        public ChannelMicroThreadAwaiter<ContactPoint> ContactEnded()
        {
            return ContactEndedChannel.Receive();
        }

        public async Task Ended()
        {
            Collision endCollision;
            do
            {
                endCollision = await ColliderA.CollisionEnded();
            }
            while (!endCollision.Equals(this));
        }

        public override bool Equals(object obj)
        {
            var other = (Collision)obj;
            return other != null && ((other.ColliderA == ColliderA && other.ColliderB == ColliderB) || (other.ColliderB == ColliderA && other.ColliderA == ColliderB));
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var result = ColliderA.GetHashCode();
                result = (result * 397) ^ ColliderB.GetHashCode();
                return result;
            }
        }

        internal bool InternalEquals(PhysicsComponent a, PhysicsComponent b)
        {
            return (ColliderA == a && ColliderB == b) || (ColliderB == a && ColliderA == b);
        }
    }
}
