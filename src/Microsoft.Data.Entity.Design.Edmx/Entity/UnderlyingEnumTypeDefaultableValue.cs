// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal class UnderlyingEnumTypeDefaultableValue : DefaultableValue<string>
    {
        internal static readonly string AttributeUnderlyingType = "UnderlyingType";

        internal UnderlyingEnumTypeDefaultableValue(EFElement parent)
            : base(parent, AttributeUnderlyingType)
        {
        }

        internal override string AttributeName
        {
            get { return AttributeUnderlyingType; }
        }

        public override string DefaultValue
        {
            get { return ModelConstants.Int32PropertyType; }
        }
    }
}
