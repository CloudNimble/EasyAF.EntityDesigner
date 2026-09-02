// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Associations
{
    internal class MappingAssociationMappingRoot : MappingEFElement
    {
        public MappingAssociationMappingRoot(EditingContext context, EFElement modelItem, MappingEFElement parent)
            : base(context, modelItem, parent)
        {
        }

        internal MappingAssociation MappingAssociation
        {
            get { return GetParentOfType(typeof(MappingAssociation)) as MappingAssociation; }
        }

        internal MappingAssociationSet MappingAssociationSet
        {
            get { return GetParentOfType(typeof(MappingAssociationSet)) as MappingAssociationSet; }
        }

        internal MappingAssociationSetEnd MappingAssociationSetEnd
        {
            get { return GetParentOfType(typeof(MappingAssociationSetEnd)) as MappingAssociationSetEnd; }
        }
    }
}
