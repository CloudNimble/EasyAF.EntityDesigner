// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
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
