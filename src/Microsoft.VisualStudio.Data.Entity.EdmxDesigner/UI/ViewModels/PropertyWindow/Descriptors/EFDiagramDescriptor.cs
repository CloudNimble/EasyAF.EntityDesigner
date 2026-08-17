// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors
{
    internal class EFDiagramDescriptor : EFAnnotatableElementDescriptor<Diagram>
    {
        [LocDescription("PropertyWindow_Description_DiagramName")]
        public override string Name
        {
            get { return base.Name; }
            set { base.Name = value; }
        }

        public override string GetComponentName()
        {
            return TypedEFElement.NormalizedNameExternal;
        }

        public override string GetClassName()
        {
            return "Diagram";
        }
    }
}
