// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal class StorageEntityContainer : BaseEntityContainer
    {
        internal StorageEntityContainer(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        protected override void PreParse()
        {
            Debug.Assert(State != EFElementState.Parsed, "this object should not already be in the parsed state");
            ClearEntitySets();
            base.PreParse();
        }

        internal override bool ParseSingleElement(ICollection<XName> unprocessedElements, XElement elem)
        {
            if (elem.Name.LocalName == EntitySet.ElementName)
            {
                EntitySet es = new StorageEntitySet(this, elem);
                AddEntitySet(es);
                es.Parse(unprocessedElements);
            }
            else
            {
                return base.ParseSingleElement(unprocessedElements, elem);
            }
            return true;
        }
    }
}
