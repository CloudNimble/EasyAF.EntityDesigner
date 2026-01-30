// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal class StorageEntityType : EntityType
    {
        internal StorageEntityType(StorageEntityModel model, XElement element)
            : base(model, element)
        {
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            if (elem.Name.LocalName == Property.ElementName)
            {
                Property prop = null;

                prop = new StorageProperty(this, elem);

                prop.Parse(unprocessedElements);
                AddProperty(prop);
            }
            else
            {
                return base.ParseSingleElement(unprocessedElements, elem);
            }
            return true;
        }
    }
}
