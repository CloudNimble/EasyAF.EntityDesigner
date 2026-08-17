// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class ChangePropertyTypeScaleCommand : UpdateDefaultableValueCommand<StringOrPrimitive<UInt32>>
    {
        public Property Property { get; set; }

        internal uint? Scale
        {
            get { return Value == null ? (uint?)null : Value.PrimitiveValue; }
        }

        public ChangePropertyTypeScaleCommand()
            : base(null, null)
        {
        }

        internal ChangePropertyTypeScaleCommand(Property property, StringOrPrimitive<UInt32> value)
            : base(property.Scale, value)
        {
            Property = property;
        }
    }
}
