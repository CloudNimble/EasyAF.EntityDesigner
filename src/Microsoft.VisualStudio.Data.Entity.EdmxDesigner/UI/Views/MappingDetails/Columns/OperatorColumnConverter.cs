// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Tables;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    internal class OperatorColumnConverter : BaseColumnConverter<OperatorColumn>
    {
        protected override void PopulateMapping(ITypeDescriptorContext context)
        {
            if (context != null)
            {
                _context = context;
            }

            Debug.Assert(_context != null, "Should have a context for the PopulateMapping call.");

            PopulateMappingForSelectedObject(_context.PropertyDescriptor as OperatorColumn);
        }

        protected override void PopulateMappingForSelectedObject(OperatorColumn selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null
                &&
                selectedObject.Element != null)
            {
                if (selectedObject.Element is MappingCondition)
                {
                    var lov = selectedObject.Element.GetListOfValues(ListOfValuesCollection.SecondColumn);
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
