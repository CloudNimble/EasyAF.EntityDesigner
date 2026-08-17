// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{
    internal abstract class ViewModelChange : CommonViewModelChange
    {
        internal virtual bool IsDiagramChange
        {
            get { return false; }
        }

        internal abstract void Invoke(CommandProcessorContext cpc);
    }
}
