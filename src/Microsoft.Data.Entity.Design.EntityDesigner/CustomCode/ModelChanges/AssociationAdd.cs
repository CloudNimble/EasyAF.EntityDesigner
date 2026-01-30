// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.EntityDesigner.ViewModel;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Entity;
using Association = Microsoft.Data.Entity.Design.EntityDesigner.ViewModel.Association;
using EntityType = Microsoft.Data.Entity.Design.Model.Entity.EntityType;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class AssociationAdd : ViewModelChange
    {
        private readonly Association _association;

        internal AssociationAdd(Association association)
        {
            _association = association;
        }

        internal override void Invoke(CommandProcessorContext cpc)
        {
            var viewModel = _association.GetRootViewModel();
            Debug.Assert(viewModel != null, "Unable to find root view model from association: " + _association.Name);

            if (viewModel != null)
            {
                EntityType s = viewModel.ModelXRef.GetExisting(_association.SourceEntityType) as EntityType;
                EntityType t = viewModel.ModelXRef.GetExisting(_association.TargetEntityType) as EntityType;

                ConceptualEntityType source = s as ConceptualEntityType;
                ConceptualEntityType target = t as ConceptualEntityType;

                Debug.Assert(s != null ? source != null : true, "EntityType is not ConceptualEntityType");
                Debug.Assert(t != null ? target != null : true, "EntityType is not ConceptualEntityType");

                Debug.Assert(source != null && target != null);
                var modelAssociation = CreateConceptualAssociationCommand.CreateAssociationAndAssociationSetWithDefaultNames(
                    cpc, source, target);
                viewModel.ModelXRef.Add(modelAssociation, _association, viewModel.EditingContext);
            }
        }

        internal override int InvokeOrderPriority
        {
            get { return 130; }
        }
    }
}
