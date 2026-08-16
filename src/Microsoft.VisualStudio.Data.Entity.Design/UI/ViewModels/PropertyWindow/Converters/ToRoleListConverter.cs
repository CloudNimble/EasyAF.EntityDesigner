// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class ToRoleListConverter : DynamicListConverter<AssociationEnd, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                NavigationProperty navigationProperty = selectedObject.WrappedItem as NavigationProperty;
                if (navigationProperty != null
                    && navigationProperty.ToRole.Status == BindingStatus.Known)
                {
                    AddMapping(navigationProperty.ToRole.Target, navigationProperty.ToRole.RefName);
                }
                else if (navigationProperty.ToRole.Target == null
                         && !string.IsNullOrEmpty(navigationProperty.ToRole.RefName))
                {
                    AddMapping(null, navigationProperty.ToRole.RefName);
                }
            }
        }
    }
}
