// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using SiliconStudio.Xenko.Engine;
using SiliconStudio.Xenko.Physics;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Xenko.Engine.Events;

namespace TopDownRPG.Gameplay
{
    public enum CollisionEventType
    {
        /// <summary>
        /// Will broadcast an event only when the collision starts
        /// </summary>
        [Display("On Start")]
        StartOnly,

        /// <summary>
        /// Will broadcast an event only when the collision ends
        /// </summary>
        [Display("On End")]
        EndOnly,

        /// <summary>
        /// Will broadcast an event both when the collision starts and when it ends
        /// </summary>
        [Display("On Start and End")]
        StartAndEnd,
    }

    public class Trigger : AsyncScript
    {
        [Display("Condition")]
        public CollisionEventType TriggerCondition { get; set; } = CollisionEventType.StartOnly;

        [DataMemberIgnore]
        public EventKey<bool> TriggerEvent = new EventKey<bool>();

        public override async Task Execute()
        {
            var trigger = Entity.Get<PhysicsComponent>();
            //            trigger.ProcessCollisions = true;

            while (Game.IsRunning)
            {
                // Wait for the next collision event
                var firstCollision = await trigger.NewCollision();

                // Filter collisions based on collision groups
                var filterAhitB = ((int)firstCollision.ColliderA.CanCollideWith) & ((int)firstCollision.ColliderB.CollisionGroup);
                var filterBhitA = ((int)firstCollision.ColliderB.CanCollideWith) & ((int)firstCollision.ColliderA.CollisionGroup);
                if (filterAhitB == 0 || filterBhitA == 0)
                    continue;

                // Broadcast the collision start event
                if (TriggerCondition == CollisionEventType.StartOnly || TriggerCondition == CollisionEventType.StartAndEnd)
                    TriggerEvent.Broadcast(true);

                if (TriggerCondition == CollisionEventType.StartOnly)
                    continue;

                // Wait for the collision to end and broadcast that event
                Func<Task> collisionEndTask = async () =>
                {
                    Collision collision;
                    do
                    {
                        collision = await trigger.CollisionEnded();
                    } while (collision != firstCollision);

                    TriggerEvent.Broadcast(false);
                };

                Script.AddTask(collisionEndTask);
            }
        }
    }
}
