// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{

    internal class RoleListItem
    {
        private readonly AssociationEnd _end;
        private readonly bool _useRoleName;

        internal RoleListItem(AssociationEnd end, bool useRoleName)
        {
            _end = end;
            _useRoleName = useRoleName;
        }

        internal AssociationEnd End => _end;

        public override string ToString()
        {
            if (_end != null && _useRoleName)
            {
                return _end.Role.Value;
            }
            else if (_end != null && _end.Type.Target != null && _useRoleName == false)
            {
                return _end.Type.Target.LocalName.Value;
            }

            return string.Empty;
        }
    }

}
