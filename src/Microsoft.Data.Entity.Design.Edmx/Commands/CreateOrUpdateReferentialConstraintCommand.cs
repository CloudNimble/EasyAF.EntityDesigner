// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class CreateOrUpdateReferentialConstraintCommand : CreateReferentialConstraintCommand
    {
        internal CreateOrUpdateReferentialConstraintCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            Association association = PrincipalEnd.Parent as Association;
            Debug.Assert(
                association != null && association == DependentEnd.Parent, "Association parent for both ends must agree and be not null");
            if (association != null)
            {
                if (association.ReferentialConstraint == null)
                {
                    // ReferentialConstraint does not exist, create it.
                    base.InvokeInternal(cpc);
                }
                else
                {
                    // Update the existing ReferentialConstraint
                    var principal = association.ReferentialConstraint.Principal;
                    var dependent = association.ReferentialConstraint.Dependent;

                    principal.Role.SetRefName(PrincipalEnd);
                    dependent.Role.SetRefName(DependentEnd);

                    List<PropertyRef> oldPrincipalPropertyRefs = principal.PropertyRefs.ToList();
                    foreach (var pref in oldPrincipalPropertyRefs)
                    {
                        CommandProcessor.InvokeSingleCommand(cpc, principal.GetDeleteCommandForChild(pref));
                    }

                    foreach (var prop in PrincipalProperties)
                    {
                        principal.AddPropertyRef(prop);
                    }

                    List<PropertyRef> oldDependentPropertyRefs = dependent.PropertyRefs.ToList();
                    foreach (var pref in oldDependentPropertyRefs)
                    {
                        CommandProcessor.InvokeSingleCommand(cpc, dependent.GetDeleteCommandForChild(pref));
                    }

                    foreach (var prop in DependentProperties)
                    {
                        dependent.AddPropertyRef(prop);
                    }
                }
            }
        }
    }
}
