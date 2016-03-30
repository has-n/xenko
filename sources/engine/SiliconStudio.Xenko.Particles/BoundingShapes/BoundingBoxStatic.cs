﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Xenko.Particles.DebugDraw;

namespace SiliconStudio.Xenko.Particles.BoundingShapes
{
    [DataContract("BoundingBoxStatic")]
    public class BoundingBoxStatic : BoundingShape
    {
        /// <summary>
        /// Lower corner of the AABB
        /// </summary>
        /// <userdoc>
        /// Lower corner of the AABB (left, bottom, back)
        /// </userdoc>
        [DataMember(20)]
        public Vector3 Minimum { get; set; } = new Vector3(-1, -1, -1);

        /// <summary>
        /// Upper corner of the AABB
        /// </summary>
        /// <userdoc>
        /// Upper corner of the AABB (right, top, front)
        /// </userdoc>
        [DataMember(40)]
        public Vector3 Maximum { get; set; } = new Vector3(1, 1, 1);

        [DataMemberIgnore]
        private BoundingBox cachedBox;

        private static void AddCornerToAabb(Vector3 corner, Quaternion rotation, ref Vector3 min, ref Vector3 max)
        {
            rotation.Rotate(ref corner);
            min.X = (corner.X < min.X) ? corner.X : min.X;
            min.Y = (corner.Y < min.Y) ? corner.Y : min.Y;
            min.Z = (corner.Z < min.Z) ? corner.Z : min.Z;
            max.X = (corner.X > max.X) ? corner.X : max.X;
            max.Y = (corner.Y > max.Y) ? corner.Y : max.Y;
            max.Z = (corner.Z > max.Z) ? corner.Z : max.Z;
        }

        public override BoundingBox GetAABB(Vector3 translation, Quaternion rotation, float scale)
        {
            if (Dirty)
            {
                var min = (Minimum + Maximum) * 0.5f;
                var max = (Minimum + Maximum) * 0.5f;

                AddCornerToAabb(new Vector3(Minimum.X, Minimum.Y, Minimum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Minimum.X, Minimum.Y, Maximum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Minimum.X, Maximum.Y, Minimum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Minimum.X, Maximum.Y, Maximum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Maximum.X, Minimum.Y, Minimum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Maximum.X, Minimum.Y, Maximum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Maximum.X, Maximum.Y, Minimum.Z), rotation, ref min, ref max);
                AddCornerToAabb(new Vector3(Maximum.X, Maximum.Y, Maximum.Z), rotation, ref min, ref max);

                cachedBox = new BoundingBox(min * scale + translation, max * scale + translation);
            }

            return cachedBox;
        }

        public override bool TryGetDebugDrawShape(out DebugDrawShape debugDrawShape, out Vector3 translation, out Quaternion rotation, out Vector3 scale)
        {
            if (!DebugDraw)
                return base.TryGetDebugDrawShape(out debugDrawShape, out translation, out rotation, out scale);

            debugDrawShape = DebugDrawShape.Cube;
            scale = (Maximum - Minimum);
            translation = (Maximum + Minimum) * 0.5f;
            rotation = Quaternion.Identity;
            return true;
        }

    }
}
