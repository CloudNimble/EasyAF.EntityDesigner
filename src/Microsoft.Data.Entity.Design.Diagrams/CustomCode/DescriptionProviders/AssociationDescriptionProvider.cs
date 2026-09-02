// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.VisualStudio.Modeling;
using Microsoft.VisualStudio.Modeling.Design;
using System.ComponentModel;

namespace Microsoft.Data.Entity.Design.Diagrams.DescriptionProviders
{

    /// <summary>
    ///     This provider is wired to the Association DomainClass so that it can provide a mock
    ///     descriptor for DSL. When any property change occurs from the diagram, DSL asks the DomainClass
    ///     if there is a TypeDescriptionProvider attached to it. It then attempts to create the TypeDescriptor,
    ///     gets the properties exposed through the TypeDescriptor, and calls SetValue on the ElementTypeDescriptor
    ///     which sets the property directly on the DomainClass. This level of indirection is a result of TFS
    ///     Work Item #430446.
    /// </summary>
    internal class AssociationDescriptionProvider : ElementTypeDescriptionProvider
    {
        protected override ElementTypeDescriptor CreateTypeDescriptor(ICustomTypeDescriptor parent, ModelElement element)
        {
            if (element is Association association)
            {
                return new AssociationDescriptor(parent, association);
            }
            return base.CreateTypeDescriptor(parent, element);
        }
    }

}
