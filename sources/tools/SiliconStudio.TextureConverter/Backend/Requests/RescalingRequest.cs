// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SiliconStudio.TextureConverter.Requests
{
    /// <summary>
    /// Request to rescale a texture (will delete every mipmaps of the texture)
    /// </summary>
    internal abstract class RescalingRequest : IRequest
    {
        public override RequestType Type { get { return RequestType.Rescaling; } }

        /// <summary>
        /// The filter to be used when rescaling
        /// </summary>
        /// <value>
        /// The filter.
        /// </value>
        public Filter.Rescaling Filter { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RescalingRequest"/> class.
        /// </summary>
        /// <param name="filter">The filter.</param>
        protected RescalingRequest(Filter.Rescaling filter)
        {
            this.Filter = filter;
        }

        /// <summary>
        /// Computes the new width of the image.
        /// </summary>
        /// <param name="texImage">The tex image.</param>
        /// <returns></returns>
        public abstract int ComputeWidth(TexImage texImage);

        /// <summary>
        /// Computes the new height of the image.
        /// </summary>
        /// <param name="texImage">The tex image.</param>
        /// <returns></returns>
        public abstract int ComputeHeight(TexImage texImage);

    }
}
