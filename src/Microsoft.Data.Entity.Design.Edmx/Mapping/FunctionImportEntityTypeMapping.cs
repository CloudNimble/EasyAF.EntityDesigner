// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Mapping
{
    internal class FunctionImportEntityTypeMapping : FunctionImportTypeMapping
    {
        internal static readonly string ElementName = "EntityTypeMapping";

        internal FunctionImportEntityTypeMapping(ResultMapping parent, XElement element)
            : base(parent, element)
        {
        }

        internal override string EFTypeName
        {
            get { return ElementName; }
        }

        internal EntityType EntityType
        {
            get { return TypeName.Target as EntityType; }
        }
    }
}
