// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
//
// Copyright 2012 Darren Kopp
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Core.Storage
{
    /// <summary>
    /// A builder for <see cref="ObjectId"/> using Murmurshash3 128 bits
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct ObjectIdBuilder
    {
        // ***************************************************************
        // NOTE: This file is shared with the AssemblyProcessor.
        // If this file is modified, the AssemblyProcessor has to be
        // recompiled separately. See build\Xenko-AssemblyProcessor.sln
        // ***************************************************************

        private readonly uint seed;
        const uint C1 = 0x239b961b;
        const uint C2 = 0xab0e9789;
        const uint C3 = 0x38b34ae5;
        const uint C4 = 0xa1e38b93;

        public ObjectIdBuilder(uint seed = 0)
        {
            this.seed = seed;

            // initialize hash values to seed values
            H1 = H2 = H3 = H4 = seed;
            currentLength = 0;

            currentBlock1 = 0;
            currentBlock2 = 0;
            currentBlock3 = 0;
            currentBlock4 = 0;
        }

        public uint Seed { get { return seed; } }

        public int Length
        {
            get
            {
                return currentLength;
            }
        }

        private uint H1;
        private uint H2;
        private uint H3;
        private uint H4;
        private int currentLength;

        private uint currentBlock1;
        private uint currentBlock2;
        private uint currentBlock3;
        private uint currentBlock4;

        public void Reset()
        {
            // initialize hash values to seed values
            H1 = H2 = H3 = H4 = Seed;
            currentLength = 0;
        }

        /// <summary>
        /// Gets the current calculated hash.
        /// </summary>
        /// <value>The current hash.</value>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ObjectId ComputeHash()
        {
            ObjectId result;
            ComputeHash(out result);
            return result;
        }

        /// <summary>
        /// Gets the current calculated hash.
        /// </summary>
        /// <value>The current hash.</value>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ComputeHash(out ObjectId result)
        {
            // create our keys and initialize to 0
            uint k1 = 0, k2 = 0, k3 = 0, k4 = 0;

            var remainder = currentLength % 16;

            fixed (uint* currentBlockStart = &currentBlock1)
            {
                var tail = (byte*)currentBlockStart;

                // determine how many bytes we have left to work with based on length
                switch (remainder)
                {
                    case 15: k4 ^= (uint)tail[14] << 16; goto case 14;
                    case 14: k4 ^= (uint)tail[13] << 8; goto case 13;
                    case 13: k4 ^= (uint)tail[12] << 0; goto case 12;
                    case 12: k3 ^= (uint)tail[11] << 24; goto case 11;
                    case 11: k3 ^= (uint)tail[10] << 16; goto case 10;
                    case 10: k3 ^= (uint)tail[9] << 8; goto case 9;
                    case 9: k3 ^= (uint)tail[8] << 0; goto case 8;
                    case 8: k2 ^= (uint)tail[7] << 24; goto case 7;
                    case 7: k2 ^= (uint)tail[6] << 16; goto case 6;
                    case 6: k2 ^= (uint)tail[5] << 8; goto case 5;
                    case 5: k2 ^= (uint)tail[4] << 0; goto case 4;
                    case 4: k1 ^= (uint)tail[3] << 24; goto case 3;
                    case 3: k1 ^= (uint)tail[2] << 16; goto case 2;
                    case 2: k1 ^= (uint)tail[1] << 8; goto case 1;
                    case 1: k1 ^= (uint)tail[0] << 0; break;
                }
            }

            var h4 = H4 ^ RotateLeft((k4 * C4), 18) * C1;
            var h3 = H3 ^ RotateLeft((k3 * C3), 17) * C4;
            var h2 = H2 ^ RotateLeft((k2 * C2), 16) * C3;
            var h1 = H1 ^ RotateLeft((k1 * C1), 15) * C2;

            var len = (uint)currentLength;
            // pipelining friendly algorithm
            h1 ^= len; h2 ^= len; h3 ^= len; h4 ^= len;

            h1 += (h2 + h3 + h4);
            h2 += h1; h3 += h1; h4 += h1;

            h1 = FMix(h1);
            h2 = FMix(h2);
            h3 = FMix(h3);
            h4 = FMix(h4);

            h1 += (h2 + h3 + h4);
            h2 += h1; h3 += h1; h4 += h1;

            fixed (void* ptr = &result)
            {
                var h = (uint*)ptr;
                *h++ = h1;
                *h++ = h2;
                *h++ = h3;
                *h = h4;
            }
        }

        /// <summary>
        /// Writes a byte to the builder.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteByte(byte value)
        {
            fixed (uint* currentBlockStart = &currentBlock1)
            {
                var currentBlock = (byte*)currentBlockStart;

                var position = currentLength++ % 16;

                currentBlock[position] = value;

                if (position == 15)
                {
                    BodyCore(currentBlock);
                }
            }
        }


        /// <summary>
        /// Writes a buffer of byte to this builder.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">buffer</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write([NotNull] byte[] buffer)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Writes a buffer of byte to this builder.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">count;Offset + Count is out of range</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write(byte[] buffer, int offset, int count)
        {
            fixed (byte* bufferStart = buffer)
            {
                Write(bufferStart + offset, count);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write([NotNull] string str)
        {
            fixed (char* strPtr = str)
                Write((byte*)strPtr, str.Length * sizeof(char));
        }

// Those routines are using Interop which are not implemented when compiling
// the assembly processor as it is it that generates them (Chicken & Egg problem).
#if !SILICONSTUDIO_ASSEMBLY_PROCESSOR
        /// <summary>
        /// Writes the specified buffer to this instance.
        /// </summary>
        /// <typeparam name="T">Type must be a struct</typeparam>
        /// <param name="data">The data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T data) where T : struct
        {
            Write(ref data);
        }

        /// <summary>
        /// Writes the specified buffer to this instance.
        /// </summary>
        /// <typeparam name="T">Type must be a struct</typeparam>
        /// <param name="buffer">The buffer.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(T[] buffer, int offset, int count) where T : struct
        {
            var ptr = (IntPtr)Interop.Fixed(buffer) + offset * Interop.SizeOf<T>();
            Write((byte*)ptr, count * Interop.SizeOf<T>());
        }
        
        /// <summary>
        /// Writes the specified buffer to this instance.
        /// </summary>
        /// <typeparam name="T">Type must be a struct</typeparam>
        /// <param name="data">The data.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Write<T>(ref T data) where T : struct
        {
            var ptr = (byte*)Interop.Fixed(ref data);
            Write(ptr, Interop.SizeOf<T>());
        }
#endif

        /// <summary>
        /// Writes a buffer of byte to this builder.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="count">The count.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">count;Offset + Count is out of range</exception>
        public void Write(byte* buffer, int length)
        {
            fixed (uint* currentBlockStart = &currentBlock1)
            {
                var currentBlock = (byte*)currentBlockStart;
                var position = currentLength % 16;

                currentLength += length;

                // Partial block to continue?
                if (position != 0)
                {
                    var remainder = 16 - position;

                    var partialLength = length;
                    if (partialLength > remainder)
                        partialLength = remainder;

                    var dest = currentBlock + position;
                    for (var copyLength = partialLength; copyLength > 0; --copyLength)
                        *dest++ = *buffer++;
                    length -= partialLength;

                    //Utilities.CopyMemory((IntPtr)currentBlock + position, (IntPtr)buffer, partialLength);
                    //buffer += partialLength;
                    //length -= partialLength;

                    if (partialLength == remainder)
                    {
                        BodyCore(currentBlock);
                    }
                }

                if (length > 0)
                {
                    var blocks = length / 16;
                    length -= blocks * 16;

                    // Main loop
                    while (blocks-- > 0)
                    {
                        BodyCore(buffer);
                        buffer += 16;
                    }

                    // Start partial block
                    for (; length > 0; --length)
                        *currentBlock++ = *buffer++;
                    //if (length > 0)
                    //{
                    //    Utilities.CopyMemory((IntPtr)currentBlock, (IntPtr)buffer, length);
                    //}
                }
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void BodyCore(byte* data)
        {
            var b = (uint*)data;

            // K1 - consume first integer
            H1 ^= RotateLeft((*b++ * C1), 15) * C2;
            H1 = (RotateLeft(H1, 19) + H2) * 5 + 0x561ccd1b;

            // K2 - consume second integer
            H2 ^= RotateLeft((*b++ * C2), 16) * C3;
            H2 = (RotateLeft(H2, 17) + H3) * 5 + 0x0bcaa747;

            // K3 - consume third integer
            H3 ^= RotateLeft((*b++ * C3), 17) * C4;
            H3 = (RotateLeft(H3, 15) + H4) * 5 + 0x96cd1c35;

            // K4 - consume fourth integer
            H4 ^= RotateLeft((*b++ * C4), 18) * C1;
            H4 = (RotateLeft(H4, 13) + H1) * 5 + 0x32ac3b17;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint RotateLeft(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint FMix(uint h)
        {
            // pipelining friendly algorithm
            h = (h ^ (h >> 16)) * 0x85ebca6b;
            h = (h ^ (h >> 13)) * 0xc2b2ae35;
            return h ^ (h >> 16);
        }
    }
}
