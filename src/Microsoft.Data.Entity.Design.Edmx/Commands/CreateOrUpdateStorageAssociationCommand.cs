// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class CreateOrUpdateStorageAssociationCommand : CreateStorageAssociationCommand
    {
        internal CreateOrUpdateStorageAssociationCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            var association = ModelHelper.FindAssociation(cpc.Artifact.StorageModel(), Name);

            if (association == null)
            {
                // If this Association does not exist, create it
                base.InvokeInternal(cpc);
            }
            else
            {
                // If the Association already exists, update it
                Debug.Assert(
                    association.AssociationEnds().Count == 2, "Association element is invalid, it should always have exactly 2 ends");
                if (association.AssociationEnds().Count == 2)
                {
                    ModelHelper.DeterminePrincipalDependentAssociationEnds(
                        association, out AssociationEnd principal, out AssociationEnd dependent,
                        ModelHelper.DeterminePrincipalDependentAssociationEndsScenario.CreateForeignKeyProperties);
                    var updatedPrincipalRoleName = ModelHelper.CreatePKAssociationEndName(PkTable.LocalName.Value);
                    var principalMultiplicity = IsNullableFk ? ModelConstants.Multiplicity_ZeroOrOne : ModelConstants.Multiplicity_One;
                    var updatedDependentRoleName = ModelHelper.CreateFKAssociationEndName(FkTable.LocalName.Value);
                    var dependentMultiplicity = DoesFkFormPk ? ModelConstants.Multiplicity_ZeroOrOne : ModelConstants.Multiplicity_Many;

                    if (string.Compare(principal.Role.Value, updatedPrincipalRoleName, StringComparison.Ordinal) != 0)
                    {
                        principal.Role.Value = updatedPrincipalRoleName;
                    }

                    if (string.Compare(dependent.Role.Value, updatedDependentRoleName, StringComparison.Ordinal) != 0)
                    {
                        dependent.Role.Value = updatedDependentRoleName;
                    }

                    principal.Type.SetRefName(PkTable);
                    dependent.Type.SetRefName(FkTable);

                    if (string.Compare(principal.Multiplicity.Value, principalMultiplicity, StringComparison.Ordinal) != 0)
                    {
                        principal.Multiplicity.Value = principalMultiplicity;
                    }

                    if (string.Compare(dependent.Multiplicity.Value, dependentMultiplicity, StringComparison.Ordinal) != 0)
                    {
                        dependent.Multiplicity.Value = dependentMultiplicity;
                    }
                }

                Association = association;
            }
        }
    }
}
