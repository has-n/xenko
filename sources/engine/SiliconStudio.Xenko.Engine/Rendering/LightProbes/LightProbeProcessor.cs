// Copyright (c) 2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

using System;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Storage;
using SiliconStudio.Xenko.Engine;

namespace SiliconStudio.Xenko.Rendering.LightProbes
{
    public class LightProbeProcessor : EntityProcessor<LightProbeComponent>
    {
        private ObjectId previousLightProbeHash;
        private bool needPositionUpdate = false;

        public LightProbeProcessor() : base(typeof(TransformComponent))
        {
        }

        /// <summary>
        /// The current light probe runtime data.
        /// </summary>
        public LightProbeRuntimeData RuntimeData { get; private set; }

        /// <summary>
        /// Light probe runtime data is auto-computed when lightprobes are added/removed.  If you move them at runtime, please call this method.
        /// </summary>
        /// <remarks>
        /// This will also update coefficients.
        /// </remarks>
        public void UpdateLightProbePositions()
        {
            RuntimeData = null;
            needPositionUpdate = false;

            // Initial load
            try
            {
                // Collect LightProbes
                var lightProbes = new FastList<LightProbeComponent>();

                foreach (var lightProbe in ComponentDatas)
                {
                    lightProbes.Add(lightProbe.Key);
                }

                // Need at least 4 light probes to form a tetrahedron
                if (lightProbes.Count < 4)
                    return;

                RuntimeData = LightProbeGenerator.GenerateRuntimeData(lightProbes);
            }
            catch
            {
                // Allow failures
                // TODO: Log
            }
        }

        /// <summary>
        /// Updates only the coefficients of the light probes (from <see cref="LightProbeComponent.Coefficients"/> to <see cref="LightProbeRuntimeData.Coefficients"/>).
        /// </summary>
        public void UpdateLightProbeCoefficients()
        {
            if (RuntimeData == null)
                return;

            LightProbeGenerator.UpdateCoefficients(RuntimeData);
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            if (needPositionUpdate)
            {
                UpdateLightProbePositions();
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, LightProbeComponent component, LightProbeComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            needPositionUpdate = true;
        }

        protected override void OnEntityComponentRemoved(Entity entity, LightProbeComponent component, LightProbeComponent data)
        {
            needPositionUpdate = true;
            base.OnEntityComponentRemoved(entity, component, data);
        }

        protected override LightProbeComponent GenerateComponentData(Entity entity, LightProbeComponent component)
        {
            return component;
        }
    }
}
