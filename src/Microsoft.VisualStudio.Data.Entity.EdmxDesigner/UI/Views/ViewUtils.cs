// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.Ide;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views
{
    internal static class ViewUtils
    {
        /// <summary>
        ///     Sets the base type, telling the user if the change would create circular inheritance.
        /// </summary>
        /// <remarks>
        ///     The edit itself lives in <see cref="InheritanceHelper.TrySetBaseEntityType" />, in the model, so the
        ///     designer can perform it without reaching into the shell. This wrapper adds the part that genuinely
        ///     needs a shell: showing the error.
        /// </remarks>
        internal static bool SetBaseEntityType(
            CommandProcessorContext cpc, ConceptualEntityType derivedEntity, ConceptualEntityType baseEntity)
        {
            if (InheritanceHelper.TrySetBaseEntityType(cpc, derivedEntity, baseEntity))
            {
                return true;
            }

            VsUtils.ShowErrorDialog(
                String.Format(
                    CultureInfo.CurrentCulture, EdmxDesignerResources.Error_CircularInheritanceAborted, derivedEntity.LocalName.Value,
                    baseEntity.LocalName.Value));

            return false;
        }

        // Fix for Dev10 Bug 592077: Display Horizontal Scroll bar if the name exceeds the container.
        internal static void DisplayHScrollOnListBoxIfNecessary(ListBox listBox)
        {
            // Display a horizontal scroll bar if necessary.
            listBox.HorizontalScrollbar = true;

            // Create a Graphics object to use when determining the size of the largest item in the ListBox.
            var g = listBox.CreateGraphics();

            // Determine the size for HorizontalExtent using the MeasureString method.
            var maxHorizontalSize = -1;
            for (var i = 0; i < listBox.Items.Count; i++)
            {
                var hzSize = (int)g.MeasureString(listBox.Items[i].ToString(), listBox.Font).Width;
                if (hzSize > maxHorizontalSize)
                {
                    maxHorizontalSize = hzSize;
                }
            }
            // Set the HorizontalExtent property.
            if (maxHorizontalSize != -1)
            {
                listBox.HorizontalExtent = maxHorizontalSize;
            }
        }
    }
}