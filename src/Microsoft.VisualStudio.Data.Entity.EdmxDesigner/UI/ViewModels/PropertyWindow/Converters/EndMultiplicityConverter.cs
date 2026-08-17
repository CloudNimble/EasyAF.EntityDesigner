// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Converters;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using System;
using System.Diagnostics;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Converters
{

    internal abstract class EndMultiplicityConverter : DynamicListConverter<string, ObjectDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(ObjectDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                if (selectedObject.WrappedItem is Association association)
                {
                    var end = GetEnd(association);
                    if (end != null)
                    {
                        var typeName = String.Empty;
                        if (end.Type.Target != null)
                        {
                            typeName = end.Type.Target.LocalName.Value;
                        }
                        AddMapping(
                            ModelConstants.Multiplicity_Many,
                            String.Format(CultureInfo.CurrentCulture, EdmxDesignerResources.PropertyWindow_Value_MultiplicityManyOf, typeName));
                        AddMapping(
                            ModelConstants.Multiplicity_One,
                            String.Format(CultureInfo.CurrentCulture, EdmxDesignerResources.PropertyWindow_Value_MultiplicityOneOf, typeName));
                        AddMapping(
                            ModelConstants.Multiplicity_ZeroOrOne,
                            String.Format(CultureInfo.CurrentCulture, EdmxDesignerResources.PropertyWindow_Value_MultiplicityZeroOrOneOf, typeName));
                    }
                }
            }
        }

        protected abstract AssociationEnd GetEnd(Association association);
    }

}