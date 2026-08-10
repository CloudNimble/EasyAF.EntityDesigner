// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Microsoft.Data.Entity.Design.EntityDesigner
{
    internal partial class InheritanceToolConnectAction
    {
        /// <summary>
        ///     Prevent Inheritance from connecting to self
        /// </summary>
        partial class InheritanceToolConnectionType
        {
            public override bool CanCreateConnection(
                ShapeElement sourceShapeElement, ShapeElement targetShapeElement, ref string connectionWarning)
            {
                if ((sourceShapeElement != null)
                    && (targetShapeElement != null))
                {
                    if (RemovePassThroughShapes(sourceShapeElement) == RemovePassThroughShapes(targetShapeElement))
                    {
                        return false;
                    }
                }
                return base.CanCreateConnection(sourceShapeElement, targetShapeElement, ref connectionWarning);
            }

            private static ShapeElement RemovePassThroughShapes(ShapeElement shape)
            {
                if (shape is Compartment)
                {
                    return shape.ParentShape;
                }
                if (shape is SwimlaneShape swimlane
                    && swimlane.ForwardDragDropToParent)
                {
                    return shape.ParentShape;
                }
                return shape;
            }
        }
    }
}
