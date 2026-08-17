// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class SetDocumentationSummaryCommand : Command
    {
        private readonly EFDocumentableItem _efElement;
        private readonly string _summaryText;

        internal SetDocumentationSummaryCommand(EFDocumentableItem efElement, string summaryText)
        {
            Debug.Assert(efElement != null, "efElement should not be null");
            Debug.Assert(efElement.HasDocumentationElement, "SetDocumentationSummary not supported for this EFElement");

            _efElement = efElement;
            _summaryText = summaryText;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            if (String.IsNullOrEmpty(_summaryText))
            {
                if (_efElement.Documentation != null
                    && _efElement.Documentation.Summary != null)
                {
                    DeleteEFElementCommand.DeleteInTransaction(cpc, _efElement.Documentation.Summary);

                    // if the documentation node is empty, delete it
                    if (_efElement.Documentation.LongDescription == null)
                    {
                        DeleteEFElementCommand.DeleteInTransaction(cpc, _efElement.Documentation);
                    }
                }
            }
            else
            {
                _efElement.Documentation ??= new Documentation(_efElement, null);

                if (_efElement.Documentation.Summary == null)
                {
                    _efElement.Documentation.Summary = new Summary(_efElement.Documentation, null);
                }

                _efElement.Documentation.Summary.Text = _summaryText;

                XmlModelHelper.NormalizeAndResolve(_efElement);
            }
        }
    }
}
