// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class FromRoleListConverter : DynamicListConverter<AssociationEnd, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                NavigationProperty navigationProperty = selectedObject.WrappedItem as NavigationProperty;
                if (navigationProperty != null
                    && navigationProperty.FromRole.Status == BindingStatus.Known)
                {
                    AddMapping(navigationProperty.FromRole.Target, navigationProperty.FromRole.RefName);
                }
                else if (navigationProperty.FromRole.Target == null
                         && !string.IsNullOrEmpty(navigationProperty.FromRole.RefName))
                {
                    AddMapping(null, navigationProperty.FromRole.RefName);
                }
            }
        }
    }
}
