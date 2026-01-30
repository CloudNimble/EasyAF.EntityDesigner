// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model.Designer;
using Microsoft.Data.Entity.Design.Model.Entity;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class CreateInheritanceConnectorCommand : Command
    {
        private readonly Diagram _diagram;
        private readonly EntityType _entity;
        private InheritanceConnector _created;

        internal CreateInheritanceConnectorCommand(Diagram diagram, EntityType entity)
        {
            CommandValidation.ValidateConceptualEntityType(entity);
            Debug.Assert(diagram != null, "diagram is null");

            _diagram = diagram;
            _entity = entity;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            InheritanceConnector inheritanceConnector = new InheritanceConnector(_diagram, null);
            _diagram.AddInheritanceConnector(inheritanceConnector);

            inheritanceConnector.EntityType.SetRefName(_entity);

            XmlModelHelper.NormalizeAndResolve(inheritanceConnector);

            _created = inheritanceConnector;
        }

        internal InheritanceConnector InheritanceConnector
        {
            get { return _created; }
        }
    }
}
