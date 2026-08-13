// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.UI.ViewModels.Explorer;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.Explorer
{
    internal class ExplorerConceptualProperty : ExplorerProperty
    {
        public ExplorerConceptualProperty(EditingContext context, Property property, ExplorerEFElement parent)
            : base(context, property, parent)
        {
            // do nothing
        }

        public override bool IsEditableInline
        {
            get
            {
                // Conceptual property names are editable inline if they are a 
                // within a Complex Type whether they are scalar properties or
                // complex properties
                Property prop = ModelItem as ConceptualProperty;
                if (null == prop)
                {
                    prop = ModelItem as ComplexConceptualProperty;
                }
                if (null != prop)
                {
                    if (prop.Parent is ComplexType ct)
                    {
                        return true;
                    }
                }

                return base.IsEditableInline;
            }
        }

        internal override string ExplorerImageResourceKeyName
        {
            get
            {
                if (ModelItem is ComplexConceptualProperty)
                {
                    return "ComplexPropertyPngIcon";
                }
                else
                {
                    if (IsKeyProperty)
                    {
                        return "PropertyKeyPngIcon";
                    }
                    else
                    {
                        return "PropertyPngIcon";
                    }
                }
            }
        }
    }
}
