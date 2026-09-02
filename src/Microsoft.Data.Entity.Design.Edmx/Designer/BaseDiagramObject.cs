// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Diagnostics;
using System.Xml.Linq;

namespace Microsoft.Data.Tools.Model.Diagram
{

    internal abstract class BaseDiagramObject : EFElement, DiagramEFObject
    {
        protected BaseDiagramObject(EFElement parent, XElement element)
            : base(parent, element)
        {
        }

        internal virtual IDiagram Diagram
        {
            get
            {
                IDiagram diagram = GetParentOfType(typeof(IDiagram)) as IDiagram;
                Debug.Assert(diagram != null, "Could not find diagram for the connector with display name:" + DisplayName);
                return diagram;
            }
        }

        internal abstract EFObject ModelItem { get; }
    }

}
