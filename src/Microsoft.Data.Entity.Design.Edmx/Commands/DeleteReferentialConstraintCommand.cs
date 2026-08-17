// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;

namespace Microsoft.Data.Entity.Design.Model.Commands
{
    internal class DeleteReferentialConstraintCommand : DeleteEFElementCommand
    {
        private List<Property> _dependentProperties;

        internal DeleteReferentialConstraintCommand(ReferentialConstraint referentialConstraint)
            : base(referentialConstraint)
        {
        }

        internal DeleteReferentialConstraintCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        internal IList<Property> DependentProperties
        {
            get { return _dependentProperties; }
        }

        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            _dependentProperties = [];
            base.PreInvoke(cpc);

            if (EFElement is ReferentialConstraint referentialConstraint
                && referentialConstraint.Dependent != null)
            {
                foreach (var property in referentialConstraint.Dependent.Properties)
                {
                    _dependentProperties.Add(property);
                }
            }
        }
    }
}
