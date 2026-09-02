// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal abstract class EntityDesignerSurfaceModelChange : ViewModelChange
    {
        private readonly EntityDesignerSurface _diagram;

        internal override bool IsDiagramChange
        {
            get { return true; }
        }

        protected EntityDesignerSurfaceModelChange(EntityDesignerSurface diagram)
        {
            _diagram = diagram;
        }

        public EntityDesignerSurface Diagram
        {
            get { return _diagram; }
        }
    }
}
