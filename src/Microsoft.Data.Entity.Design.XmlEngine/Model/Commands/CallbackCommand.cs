// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.Commands
{
    internal class CallbackCommand : Command
    {
        private readonly Action _delegateCommandCallback;

        internal CallbackCommand(Action delegateCommandCallback)
        {
            _delegateCommandCallback = delegateCommandCallback;
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            _delegateCommandCallback();
        }
    }
}
