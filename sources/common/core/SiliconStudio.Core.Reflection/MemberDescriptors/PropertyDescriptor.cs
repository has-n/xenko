// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// A <see cref="IMemberDescriptor"/> for a <see cref="PropertyInfo"/>
    /// </summary>
    public class PropertyDescriptor : MemberDescriptorBase
    {
        private readonly MethodInfo getMethod;
        private readonly MethodInfo setMethod;

        public PropertyDescriptor(ITypeDescriptor typeDescriptor, PropertyInfo propertyInfo, StringComparer defaultNameComparer)
            : base(propertyInfo, defaultNameComparer)
        {
            if (propertyInfo == null) throw new ArgumentNullException(nameof(propertyInfo));

            PropertyInfo = propertyInfo;

            getMethod = propertyInfo.GetGetMethod(false) ?? propertyInfo.GetGetMethod(true);
            if (propertyInfo.CanWrite && propertyInfo.GetSetMethod(!IsPublic) != null)
            {
                setMethod = propertyInfo.GetSetMethod(!IsPublic);
            }
            TypeDescriptor = typeDescriptor;
        }

        /// <summary>
        /// Gets the property information attached to this instance.
        /// </summary>
        /// <value>The property information.</value>
        public PropertyInfo PropertyInfo { get; }

        public override Type Type => PropertyInfo.PropertyType;

        public sealed override bool IsPublic => getMethod?.IsPublic ?? false;

        public override bool HasSet => setMethod != null;

        public override object Get(object thisObject)
        {
            return getMethod.Invoke(thisObject, null);
        }

        public override void Set(object thisObject, object value)
        {
            if (!HasSet)
                throw new InvalidOperationException($"The property [{Name}] of type [{DeclaringType.Name}] has no setter.");

            setMethod.Invoke(thisObject, new[] {value});
        }

        public override IEnumerable<T> GetCustomAttributes<T>(bool inherit)
        {
            return PropertyInfo.GetCustomAttributes<T>(inherit);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return $"Property [{Name}] from Type [{(PropertyInfo.DeclaringType != null ? PropertyInfo.DeclaringType.FullName : string.Empty)}]";
        }
    }
}
