// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.Functions
{
    internal class MappingFunctionMappingRoot : MappingEFElement
    {
        public MappingFunctionMappingRoot(EditingContext context, EFElement modelItem, MappingEFElement parent)
            : base(context, modelItem, parent)
        {
        }

        internal MappingFunctionEntityType MappingFunctionEntityType
        {
            get { return GetParentOfType(typeof(MappingFunctionEntityType)) as MappingFunctionEntityType; }
        }

        internal MappingModificationFunctionMapping MappingModificationFunctionMapping
        {
            get { return GetParentOfType(typeof(MappingModificationFunctionMapping)) as MappingModificationFunctionMapping; }
        }
    }
}
