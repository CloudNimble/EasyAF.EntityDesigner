// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.View;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal abstract class AssociationConnectorModelChange : ViewModelChange
    {
        private readonly AssociationConnector _associationConnector;

        internal override bool IsDiagramChange
        {
            get { return true; }
        }

        protected AssociationConnectorModelChange(AssociationConnector associationConnector)
        {
            _associationConnector = associationConnector;
        }

        public AssociationConnector AssociationConnector
        {
            get { return _associationConnector; }
        }
    }
}
