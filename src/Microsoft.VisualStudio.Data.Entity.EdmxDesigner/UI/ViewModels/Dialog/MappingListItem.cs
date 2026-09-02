// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Model;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{

    internal class MappingListItem
    {
        private readonly Symbol _principalSymbol;

        internal bool IsValidPrincipalKey { get; private set; }

        internal MappingListItem(Symbol principalSymbol, Symbol dependentSymbol, bool isValidPrincipalKey)
        {
            _principalSymbol = principalSymbol;
            DependentProperty = dependentSymbol;
            IsValidPrincipalKey = isValidPrincipalKey;
        }

        internal Symbol PrincipalKey => _principalSymbol;

        internal Symbol DependentProperty { get; set; }

        internal int CurrentIndex { get; set; }

        public override string ToString()
        {
            if (_principalSymbol != null)
            {
                return _principalSymbol.GetLocalName();
            }
            return string.Empty;
        }
    }

}
