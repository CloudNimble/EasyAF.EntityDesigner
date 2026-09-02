// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class SetDocumentationLongDescriptionCommand : Command
    {
        private readonly EFDocumentableItem _efElement;
        private readonly string _longDescriptionText;

        internal SetDocumentationLongDescriptionCommand(EFDocumentableItem efElement, string longDescriptionText)
        {
            Debug.Assert(efElement != null, "efElement should not be null");
            Debug.Assert(efElement.HasDocumentationElement, "SetDocumentationSummary not supported for this EFElement");

            _efElement = efElement;
            _longDescriptionText = longDescriptionText;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            if (String.IsNullOrEmpty(_longDescriptionText))
            {
                if (_efElement.Documentation != null
                    && _efElement.Documentation.LongDescription != null)
                {
                    DeleteEFElementCommand.DeleteInTransaction(cpc, _efElement.Documentation.LongDescription);

                    // if the documentation node is empty, delete it
                    if (_efElement.Documentation.Summary == null)
                    {
                        DeleteEFElementCommand.DeleteInTransaction(cpc, _efElement.Documentation);
                    }
                }
            }
            else
            {
                _efElement.Documentation ??= new Documentation(_efElement, null);

                if (_efElement.Documentation.LongDescription == null)
                {
                    _efElement.Documentation.LongDescription = new LongDescription(_efElement.Documentation, null);
                }

                _efElement.Documentation.LongDescription.Text = _longDescriptionText;

                XmlModelHelper.NormalizeAndResolve(_efElement);
            }
        }
    }
}
