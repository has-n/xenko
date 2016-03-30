﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Particles.DebugDraw;
using SiliconStudio.Xenko.Particles.Updaters.FieldShapes;

namespace SiliconStudio.Xenko.Particles.Modules
{
    /// <summary>
    /// The <see cref="UpdaterCollider"/> is an updater which tests the particles against a preset surface or shape, making them bounce off it when they collide
    /// </summary>
    [DataContract("UpdaterCollider")]
    [Display("Collider")]
    public class UpdaterCollider : ParticleUpdater
    {
        /// <summary>
        /// Default constructor which also registers the fields required by this updater
        /// </summary>
        public UpdaterCollider()
        {
            // The collider shape operates over the particle's position and velocity, updating them as required
            RequiredFields.Add(ParticleFields.Position);
            RequiredFields.Add(ParticleFields.Velocity);
            RequiredFields.Add(ParticleFields.Life);

            DisplayParticlePosition = true;
            DisplayParticleRotation = true;
            DisplayParticleScale = true;
        }


        /// <inheritdoc/>
        [DataMemberIgnore]
        public override bool IsPostUpdater => true;

        /// <summary>
        /// The type of the shape which defines this collider
        /// </summary>
        /// <userdoc>
        /// The type of the shape which defines this collider
        /// </userdoc>
        [DataMember(10)]
        [Display("Shape")]
        public FieldShape FieldShape { get; set; }

        /// <summary>
        /// Shows if the collider shape is hollow on the inside or solid
        /// </summary>
        /// <userdoc>
        /// If the collider shape is hollow, particles can't escape it.
        /// If the collider shape is solid, particles can't enter the shape.
        /// </userdoc>
        [DataMember(50)]
        [Display("Is hollow")]
        public bool IsHollow { get; set; } = false;

        /// <summary>
        /// Kill particles when they collide with the shape
        /// </summary>
        /// <userdoc>
        /// Kill particles when they collide with the shape
        /// </userdoc>
        [DataMember(60)]
        [Display("Kill particles")]
        public bool KillParticles { get; set; } = false;

        /// <summary>
        /// How much of the vertical (normal-oriented) kinetic energy is preserved after impact
        /// </summary>
        /// <userdoc>
        /// How much of the vertical (normal-oriented) kinetic energy is preserved after impact.
        /// Restitution here only affects vertical velocity (perpendicular to the surface).
        /// </userdoc>
        [DataMember(70)]
        [DataMemberRange(0, 1, 0.001, 0.1)]
        [Display("Restitution")]
        public float Restitution { get; set; } = 0.5f;

        /// <summary>
        /// How much of the horizontal (normal-perpendicular) kinetic energy is lost after impact
        /// </summary>
        /// <userdoc>
        /// How much of the horizontal (normal-perpendicular) kinetic energy is lost after impact
        /// Friction here only affects horizonal velocity (along the surface).
        /// </userdoc>
        [DataMember(90)]
        [DataMemberRange(0, 1, 0.001, 0.1)]
        [Display("Friction")]
        public float Friction { get; set; } = 0.1f;


        /// <inheritdoc/>
        public override unsafe void Update(float dt, ParticlePool pool)
        {
            if (!pool.FieldExists(ParticleFields.Position) || !pool.FieldExists(ParticleFields.Velocity))
                return;

            var posField = pool.GetField(ParticleFields.Position);
            var velField = pool.GetField(ParticleFields.Velocity);
            var lifeField = pool.GetField(ParticleFields.Life);

            foreach (var particle in pool)
            {
                var surfacePoint = new Vector3(0, 0, 0);
                var surfaceNormal = new Vector3(0, 1, 0);

                var particlePos = (*((Vector3*)particle[posField]));
                var particleVel = (*((Vector3*)particle[velField]));

                var isInside = false;
                if (FieldShape != null)
                {
                    FieldShape.PreUpdateField(WorldPosition, WorldRotation, WorldScale);

                    isInside = FieldShape.IsPointInside(particlePos, out surfacePoint, out surfaceNormal);
                }

                if (IsHollow != isInside)
                {
                    if (IsHollow)
                        surfaceNormal *= -1;

                    // The particle is on the wrong side of the collision shape and must collide
                    (*((Vector3*)particle[posField])) = surfacePoint;

                    var verIncidentCoef = Vector3.Dot(surfaceNormal, particleVel);

                    var verticalIncidentVelocity = verIncidentCoef * surfaceNormal;
                    var horizontalIncidentVelocity = particleVel - verticalIncidentVelocity;

                    particleVel = horizontalIncidentVelocity * (1 - Friction) + 
                        verticalIncidentVelocity * ((verIncidentCoef > 0) ? Restitution : -Restitution);

                    (*((Vector3*)particle[velField])) = particleVel;


                    // TODO Maybe set some collision flags if other calculations depend on them

                    // Possibly kill the particle
                    if (KillParticles)
                    {
                        // Don't set the particle's life directly to 0 just yet - it might need to spawn other particles on impact
                        (*((float*)particle[lifeField])) = MathUtil.ZeroTolerance;
                    }
                }
            }
        }

        /// <summary>
        /// Should this Particle Module's bounds be displayed as a debug draw
        /// </summary>
        /// <userdoc>
        /// Display the Particle Module's bounds as a wireframe debug shape. Temporary feature (will be removed later)!
        /// </userdoc>
        [DataMember(-1)]
        [DefaultValue(false)]
        public bool DebugDraw { get; set; } = false;

        /// <inheritdoc/>
        public override bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            if (!DebugDraw)
                return base.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale);

            rotation = Quaternion.Identity;
            scale = new Vector3(1, 1, 1);
            translation = new Vector3(0, 0, 0);

            debugDrawShape = FieldShape?.GetDebugDrawShape(out translation, out rotation, out scale) ?? DebugDrawShape.None;

            rotation *= WorldRotation;

            scale *= WorldScale;

            translation *= WorldScale;
            rotation.Rotate(ref translation);
            translation += WorldPosition;

            return true;
        }
    }
}
