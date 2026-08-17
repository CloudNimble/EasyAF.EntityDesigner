// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Model.Entity
{
    internal class LongDescription : TextNode
    {
        internal static readonly string ElementName = "LongDescription";

        internal LongDescription(EFContainer parent, XElement element)
            : base(parent, element)
        {
        }
    }
}
