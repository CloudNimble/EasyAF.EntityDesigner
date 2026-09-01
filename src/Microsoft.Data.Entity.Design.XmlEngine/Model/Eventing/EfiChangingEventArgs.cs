// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;

namespace Microsoft.Data.Entity.Design.XmlEngine.Model.Eventing
{

    internal class EfiChangingEventArgs : EventArgs
    {
        private readonly CommandProcessorContext _cpc;

        internal EfiChangingEventArgs(CommandProcessorContext cpc)
        {
            _cpc = cpc;
        }

        public CommandProcessorContext CommandProcessorContext
        {
            get { return _cpc; }
        }
    }

}
