// Copyright (c) 2016-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.

/* MIT License

Copyright (c) 2016 JetBrains http://www.jetbrains.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE. */

using System;

namespace SiliconStudio.Core.Annotations
{
    /// <summary>
    /// When applied to a target attribute, specifies a requirement for any type marked
    /// with the target attribute to implement or inherit specific type or types.
    /// </summary>
    /// <example>
    /// <code>
    /// [BaseTypeRequired(typeof(IComponent)] // Specify requirement
    /// class ComponentAttribute : Attribute { }
    /// 
    /// [Component] // ComponentAttribute requires implementing IComponent interface
    /// class MyComponent : IComponent { }
    /// </code>
    /// </example>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [BaseTypeRequired(typeof(Attribute))]
    public sealed class BaseTypeRequiredAttribute : Attribute
    {
        public BaseTypeRequiredAttribute([NotNull] Type baseType)
        {
            BaseType = baseType;
        }

        [NotNull]
        public Type BaseType { get; private set; }
    }
}
