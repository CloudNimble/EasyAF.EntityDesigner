// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Tables
{
    internal class MappingEntityMappingRoot : MappingEFElement
    {
        public MappingEntityMappingRoot(EditingContext context, EFElement modelItem, MappingEFElement parent)
            : base(context, modelItem, parent)
        {
        }

        internal MappingConceptualEntityType MappingConceptualEntityType
        {
            get { return GetParentOfType(typeof(MappingConceptualEntityType)) as MappingConceptualEntityType; }
        }

        internal MappingStorageEntityType MappingStorageEntityType
        {
            get { return GetParentOfType(typeof(MappingStorageEntityType)) as MappingStorageEntityType; }
        }
    }
}
