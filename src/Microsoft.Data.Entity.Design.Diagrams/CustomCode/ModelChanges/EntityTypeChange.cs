// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Diagrams.DomainClasses;
using Microsoft.Data.Entity.Design.Diagrams.Rules;
using Microsoft.Data.Entity.Design.Diagrams.ViewModel;
using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Diagrams.ModelChanges
{
    internal class EntityTypeChange : ViewModelChange
    {
        private readonly EntityType _entityType;

        internal EntityTypeChange(EntityType entityType)
        {
            _entityType = entityType;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _entityType.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from entity type: " + _entityType.Name);

            if (viewModel != null)
            {
                Edmx.Entity.EntityType entityType = viewModel.ModelXRef.GetExisting(_entityType) as Edmx.Entity.EntityType;
                Debug.Assert(entityType != null, "entityType != null");
                Command c = new EntityDesignRenameCommand(entityType, _entityType.Name, true);
                CommandProcessor cp = new CommandProcessor(cpc, c);
                cp.Invoke();
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 200; }
        }
    }
}
