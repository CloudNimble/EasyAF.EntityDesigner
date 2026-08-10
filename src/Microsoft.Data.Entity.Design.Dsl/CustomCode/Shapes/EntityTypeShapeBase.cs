// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Drawing;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.VisualStudio.Modeling;

namespace Microsoft.Data.Entity.Design.EntityDesigner.View
{
    internal abstract partial class EntityTypeShapeBase
    {
        private static string GetDisplayPropertyFromEntityTypeForProperties(ModelElement element)
        {
            if (element is not Property property)
            {
                return string.Empty;
            }

            if (property.EntityType.EntityDesignerViewModel.GetDiagram().DisplayNameAndType)
            {
                return property.Name + " : " + property.Type;
            }

            return property.Name;
        }

        /// <summary>
        ///     EntityTypeShape method to receive notification of changes to FillColor.
        /// </summary>
        protected abstract void OnFillColorChanged(Color newValue);

        internal sealed partial class FillColorPropertyHandler
        {
            /// <summary>
            ///     Hookup to value handler method to get notified when FillColor value has changed.
            /// </summary>
            protected override void OnValueChanged(EntityTypeShapeBase element, Color oldValue, Color newValue)
            {
                base.OnValueChanged(element, oldValue, newValue);
                element.OnFillColorChanged(newValue);
            }
        }
    }
}
