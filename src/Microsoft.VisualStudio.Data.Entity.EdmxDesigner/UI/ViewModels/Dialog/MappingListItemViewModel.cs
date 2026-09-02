// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using System.Globalization;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{

    /// <summary>
    /// ViewModel for displaying MappingListItem in the ListView.
    /// </summary>
    internal class MappingListItemViewModel
    {
        private readonly MappingListItem _item;
        private readonly EFArtifact _dependentArtifact;

        internal MappingListItemViewModel(MappingListItem item, EFArtifact dependentArtifact)
        {
            _item = item;
            _dependentArtifact = dependentArtifact;
        }

        internal MappingListItem MappingItem => _item;

        internal bool IsValidPrincipalKey => _item.IsValidPrincipalKey;

        public string PrincipalKeyDisplay
        {
            get
            {
                if (_item.IsValidPrincipalKey == false)
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        EdmxDesignerResources.RefConstraintDialog_ErrorInRCPrincipalProperty,
                        _item.PrincipalKey.GetLocalName());
                }
                return _item.PrincipalKey.GetLocalName();
            }
        }

        public string DependentPropertyDisplay
        {
            get
            {
                if (_item.DependentProperty == null)
                {
                    return string.Empty;
                }

                if (_dependentArtifact?.ArtifactSet.LookupSymbol(_item.DependentProperty) is Property)
                {
                    return _item.DependentProperty.GetLocalName();
                }
                else
                {
                    return string.Format(
                        CultureInfo.CurrentCulture,
                        EdmxDesignerResources.RefConstraintDialog_ErrorInRCDependentProperty,
                        _item.DependentProperty.GetLocalName());
                }
            }
        }
    }

}
