// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Designer;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class CreateAssociationConnectorCommand : Command
    {
        private readonly Diagram _diagram;
        private readonly Association _association;
        private AssociationConnector _created;

        internal CreateAssociationConnectorCommand(Diagram diagram, Association association)
        {
            CommandValidation.ValidateAssociation(association);
            Debug.Assert(diagram != null, "diagram is null");

            _diagram = diagram;
            _association = association;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            AssociationConnector associationConnector = new AssociationConnector(_diagram, null);
            _diagram.AddAssociationConnector(associationConnector);

            associationConnector.Association.SetRefName(_association);

            XmlModelHelper.NormalizeAndResolve(associationConnector);

            _created = associationConnector;
        }

        internal AssociationConnector AssociationConnector
        {
            get { return _created; }
        }
    }
}
