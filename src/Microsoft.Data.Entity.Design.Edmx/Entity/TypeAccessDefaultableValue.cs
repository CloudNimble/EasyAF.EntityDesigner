// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{
    internal class TypeAccessDefaultableValue : DefaultableValue<string>
    {
        internal static readonly string AttributeTypeAccess = "TypeAccess";

        internal TypeAccessDefaultableValue(EFElement parent)
            : base(parent, AttributeTypeAccess, SchemaManager.GetCodeGenerationNamespaceName())
        {
        }

        internal override string AttributeName
        {
            get { return AttributeTypeAccess; }
        }

        public override string DefaultValue
        {
            get { return ModelConstants.CodeGenerationAccessPublic; }
        }
    }
}
