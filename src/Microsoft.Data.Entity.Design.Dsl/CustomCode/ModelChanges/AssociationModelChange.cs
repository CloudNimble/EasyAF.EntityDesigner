// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.UI.Views.Dialogs;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class Association_AddFromDialog : ViewModelChange
    {
        private readonly NewAssociationDialog _dialog;

        internal Association_AddFromDialog(NewAssociationDialog dialog)
        {
            _dialog = dialog;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            CreateConceptualAssociationCommand cmd = new CreateConceptualAssociationCommand(
                _dialog.AssociationName,
                _dialog.End1Entity,
                _dialog.End1Multiplicity,
                _dialog.End1NavigationPropertyName,
                _dialog.End2Entity,
                _dialog.End2Multiplicity,
                _dialog.End2NavigationPropertyName,
                false, // uniquify names
                _dialog.CreateForeignKeyProperties);
            CommandProcessor.InvokeSingleCommand(cpc, cmd);
            Debug.Assert(cmd.CreatedAssociation != null);
        }

        internal override int InvokeOrderPriority
        {
            get { return 130; }
        }
    }
}
