// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.Design.UI.Views.Dialogs;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.PropertyWindow.Editors
{
    internal class DebugViewerEditor : ObjectSelectorEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            if (context == null
                || context.Instance == null)
            {
                return value;
            }

            var dlg = new DebugViewerDialog(context.PropertyDescriptor.Name, value as string);
            dlg.ShowModal();

            return value;
        }
    }
}
