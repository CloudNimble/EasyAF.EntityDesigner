// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors;
using EFExtensions = Microsoft.Data.Entity.Design.Model.EFExtensions;
using XmlDesignerBaseResources = Microsoft.Data.Tools.XmlDesignerBase.Resources;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Converters
{
    internal class FuncImpSprocConverter : DynamicListConverter<Function, EFFunctionImportDescriptor>
    {
        protected override void PopulateMappingForSelectedObject(EFFunctionImportDescriptor selectedObject)
        {
            Debug.Assert(selectedObject != null, "selectedObject should not be null");

            if (selectedObject != null)
            {
                AddMapping(null, XmlDesignerBaseResources.NoneDisplayValueUsedForUX);
                if (selectedObject.WrappedItem is FunctionImport currentType
                    && currentType.Artifact != null
                    && EFExtensions.StorageModel(currentType.Artifact) != null)
                {
                    var functions = EFExtensions.StorageModel(currentType.Artifact).Functions();
                    foreach (var function in functions)
                    {
                        AddMapping(function, function.LocalName.Value);
                    }
                }
            }
        }
    }
}