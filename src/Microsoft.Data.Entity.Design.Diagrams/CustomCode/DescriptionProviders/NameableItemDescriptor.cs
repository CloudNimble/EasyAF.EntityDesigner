// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling.Design;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.DescriptionProviders
{

    /// <summary>
    ///     The NameableItemDescriptor simply exposes the Name property since this is the only property that DSL
    ///     attempts to route through a PropertyDescriptor. DSL sets the property value on the
    ///     ElementPropertyDescriptor, which consequently sets it directly on the DomainClass.
    /// </summary>
    internal class NameableItemDescriptor : ElementTypeDescriptor
    {
        public NameableItemDescriptor(ICustomTypeDescriptor parent, NameableItem nameableItem)
            : base(parent, nameableItem)
        {
        }

        public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
        {
            PropertyDescriptorCollection propertyCollection = new PropertyDescriptorCollection(null);

            var propertyInfo = ModelElement.Store.DomainDataDirectory.FindDomainProperty(NameableItem.NameDomainPropertyId);
            Debug.Assert(
                propertyInfo != null, "We should have found the NameableItem Name's DomainPropertyId to create the PropertyDescriptor");
            if (propertyInfo != null)
            {
                propertyCollection.Add(CreatePropertyDescriptor(ModelElement, propertyInfo, attributes));
            }

            return propertyCollection;
        }
    }

}
