// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.FunctionImports
{
    internal class MappingFunctionImportMappingRoot : MappingEFElement
    {
        public MappingFunctionImportMappingRoot(EditingContext context, EFElement modelItem, MappingEFElement parent)
            : base(context, modelItem, parent)
        {
        }

        internal MappingFunctionImport MappingFunctionImport
        {
            get { return GetParentOfType(typeof(MappingFunctionImport)) as MappingFunctionImport; }
        }
    }
}
