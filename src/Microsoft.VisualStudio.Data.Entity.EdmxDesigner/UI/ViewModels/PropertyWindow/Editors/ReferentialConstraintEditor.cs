// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.Dialogs;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Descriptors;
using System;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing.Design;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.PropertyWindow.Editors
{
    internal class ReferentialConstraintEditor : ObjectSelectorEditor
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

            if (context.Instance is EFAssociationDescriptor desc)
            {
                if (desc.WrappedItem is Association assoc)
                {
                    var commands = ReferentialConstraintDialog.LaunchReferentialConstraintDialog(assoc);
                    CommandProcessorContext cpc = new CommandProcessorContext(
                        desc.EditingContext,
                        EfiTransactionOriginator.PropertyWindowOriginatorId,
                        EdmxDesignerResources.Tx_ReferentialContraint);
                    CommandProcessor cp = new CommandProcessor(cpc);
                    foreach (var c in commands)
                    {
                        cp.EnqueueCommand(c);
                    }
                    cp.Invoke();
                }
            }

            return value;
        }
    }
}