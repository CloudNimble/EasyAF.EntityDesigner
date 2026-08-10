// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.EntityDesigner.View;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
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
