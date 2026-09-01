// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class ChangeStorageEntitySetTableCommand : Command
    {
        internal ChangeStorageEntitySetTableCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        internal string NewTableName { get; set; }

        internal string OldTableName { get; private set; }

        internal StorageEntitySet StorageEntitySet { get; set; }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            StorageEntitySet.Table.Value = NewTableName;
        }

        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            base.PreInvoke(cpc);
            OldTableName = StorageEntitySet.Table.Value;
        }
    }
}
