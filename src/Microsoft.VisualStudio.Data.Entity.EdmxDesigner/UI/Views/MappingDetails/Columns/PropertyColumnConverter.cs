// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Associations;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.FunctionImports;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Functions;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    internal class PropertyColumnConverter : BaseColumnConverter<PropertyColumn>
    {
        protected override void PopulateMapping(ITypeDescriptorContext context)
        {
            if (context != null)
            {
                _context = context;
            }

            Debug.Assert(_context != null, "Should have a context for the PopulateMapping call.");

            PopulateMappingForSelectedObject(_context.PropertyDescriptor as PropertyColumn);
        }

        protected override void PopulateMappingForSelectedObject(PropertyColumn selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null
                && selectedObject.Element != null)
            {
                if (selectedObject.Element is MappingAssociationSet
                    || selectedObject.Element is MappingFunctionImport)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.FirstColumn);
                    foreach (var key in lov.Keys)
                    {
                        AddMapping(key, lov[key]);
                    }
                    return;
                }

                if (selectedObject.Element is MappingFunctionScalarProperty
                    || selectedObject.Element is MappingResultBinding)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.ThirdColumn);
                    foreach (var key in lov.Keys)
                    {
                        AddMapping(key, lov[key]);
                    }
                    return;
                }
            }
        }
    }

}
