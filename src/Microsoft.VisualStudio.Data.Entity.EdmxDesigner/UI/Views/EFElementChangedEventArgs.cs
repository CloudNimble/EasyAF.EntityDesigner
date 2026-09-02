// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views
{
    internal class EFElementChangedEventArgs : EventArgs
    {
        private readonly Uri _itemUri;

        public Uri ItemUri
        {
            get { return _itemUri; }
        }

        internal EFElementChangedEventArgs(Uri itemUri)
        {
            _itemUri = itemUri;
        }
    }
}
