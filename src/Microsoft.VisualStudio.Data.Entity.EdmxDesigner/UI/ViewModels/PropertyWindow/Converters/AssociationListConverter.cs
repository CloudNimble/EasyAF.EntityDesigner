// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using System.Diagnostics;
using XmlDesignerBaseResources = Microsoft.Data.Entity.Design.XmlEngine.XmlEngineResources;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{
    internal class AssociationListConverter : DynamicListConverter<Association, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                // Add an entry for (None) with null value
                AddMapping(null, XmlDesignerBaseResources.NoneDisplayValueUsedForUX);

                if (selectedObject.WrappedItem is NavigationProperty property
                    && property.Parent != null)
                {
                    foreach (var associationEnd in property.Parent.GetAntiDependenciesOfType<AssociationEnd>())
                    {
                        if (associationEnd.Parent is Association association
                            && !ContainsMapping(association.DisplayName))
                        {
                            AddMapping(association, association.DisplayName);
                        }
                    }
                }
            }
        }
    }
}