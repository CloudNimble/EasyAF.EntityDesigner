// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.EntityDesigner.View;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal abstract class InheritanceConnectorModelChange : ViewModelChange
    {
        private readonly InheritanceConnector _inheritanceConnector;

        internal override bool IsDiagramChange
        {
            get { return true; }
        }

        protected InheritanceConnectorModelChange(InheritanceConnector inheritanceConnector)
        {
            _inheritanceConnector = inheritanceConnector;
        }

        public InheritanceConnector InheritanceConnector
        {
            get { return _inheritanceConnector; }
        }
    }
}
