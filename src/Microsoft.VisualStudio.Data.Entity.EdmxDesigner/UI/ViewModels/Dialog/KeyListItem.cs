// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{

    internal class KeyListItem
    {
        private readonly Symbol _key;

        internal KeyListItem(Symbol key)
        {
            _key = key;
        }

        internal Symbol Key => _key;

        public override string ToString()
        {
            if (_key != null)
            {
                return _key.GetLocalName();
            }
            return string.Empty;
        }
    }

}
