// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;

namespace SiliconStudio.Xenko.Graphics
{
    /// <summary>
    /// Storage area for <see cref="DescriptorSet"/>.
    /// </summary>
    public partial class DescriptorPool : GraphicsResourceBase
    {
        public static DescriptorPool New(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts)
        {
            return new DescriptorPool(graphicsDevice, counts);
        }

#if SILICONSTUDIO_XENKO_GRAPHICS_API_DIRECT3D11 || SILICONSTUDIO_XENKO_GRAPHICS_API_OPENGL || (SILICONSTUDIO_XENKO_GRAPHICS_API_VULKAN && SILICONSTUDIO_XENKO_GRAPHICS_NO_DESCRIPTOR_COPIES)
        internal DescriptorSetEntry[] Entries;
        private int descriptorAllocationOffset;

        private DescriptorPool(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts)
        {
            // For now, we put everything together so let's compute total count
            var totalCount = 0;
            foreach (var count in counts)
            {
                totalCount += count.Count;
            }

            Entries = new DescriptorSetEntry[totalCount];
        }

        protected override void Destroy()
        {
            Entries = null;
            base.Destroy();
        }

        public void Reset()
        {
            Array.Clear(Entries, 0, descriptorAllocationOffset);
            descriptorAllocationOffset = 0;
        }

        internal int Allocate(int size)
        {
            if (descriptorAllocationOffset + size > Entries.Length)
                return -1;

            var result = descriptorAllocationOffset;
            descriptorAllocationOffset += size;
            return result;
        }
#endif
    }
}
