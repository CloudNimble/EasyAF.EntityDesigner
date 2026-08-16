// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class ComplexTypeConverter : DynamicListConverter<ComplexType, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                if (selectedObject.WrappedItem is ComplexConceptualProperty property)
                {
                    ConceptualEntityModel model = property.EntityModel as ConceptualEntityModel;
                    Debug.Assert(model != null, "Unexpected model type");
                    if (model != null)
                    {
                        // Now get all complex types to be displayed
                        foreach (var type in model.ComplexTypes())
                        {
                            AddMapping(type, type.LocalName.Value);
                        }
                    }

                    // display value for unresolved Complex Type reference
                    _displayValueForNull = property.TypeName;
                }
            }
        }
    }
}
