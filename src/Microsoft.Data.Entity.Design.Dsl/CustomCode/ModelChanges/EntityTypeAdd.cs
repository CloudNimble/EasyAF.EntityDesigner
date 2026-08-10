// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.EntityDesigner.Rules;
using Microsoft.Data.Entity.Design.Model.Commands;

namespace Microsoft.Data.Entity.Design.EntityDesigner.ModelChanges
{
    internal class EntityTypeAdd : ViewModelChange
    {
        internal override void Invoke(CommandProcessorContext cpc)
        {
            CreateEntityTypeCommand.CreateEntityTypeAndEntitySetWithDefaultNames(cpc);
        }

        internal override int InvokeOrderPriority
        {
            get { return 100; }
        }
    }
}
