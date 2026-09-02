// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Microsoft.VisualStudio.Data.Entity.Package
{

    /// <summary>
    /// Result of a hit test on the diagram.
    /// </summary>
    internal class DiagramHitResult
    {
        public DiagramHitTarget Target { get; set; }
        public ShapeElement Shape { get; set; }
        public object ModelElement { get; set; }
        /// <summary>The compartment containing the clicked property (if applicable)</summary>
        public ElementListCompartment Compartment { get; set; }
        /// <summary>The index of the clicked item within the compartment (if applicable)</summary>
        public int CompartmentItemIndex { get; set; } = -1;
    }

}
