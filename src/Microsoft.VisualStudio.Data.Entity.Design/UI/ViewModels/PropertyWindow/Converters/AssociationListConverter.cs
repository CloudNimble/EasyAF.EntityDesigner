// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using XmlDesignerBaseResources = Microsoft.Data.Tools.XmlDesignerBase.Resources;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
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