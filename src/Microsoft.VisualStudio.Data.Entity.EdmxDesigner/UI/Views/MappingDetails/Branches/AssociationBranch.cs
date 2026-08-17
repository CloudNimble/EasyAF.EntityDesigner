// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Associations;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Branches
{
    internal class AssociationBranch : HeaderBranch
    {
        private MappingAssociation _mappingAssociation;
        private TreeGridDesignerColumnDescriptor[] _columns;

        // <summary>
        //     ITreeGridDesignerInitializeBranch
        // </summary>
        public override bool Initialize(object component, TreeGridDesignerColumnDescriptor[] columns)
        {
            if (!base.Initialize(component, columns))
            {
                return false;
            }

            _mappingAssociation = component as MappingAssociation;
            if (_mappingAssociation != null)
            {
                _columns = columns;
                PopulateHeaders(true);
                return true;
            }

            return false;
        }

        private void PopulateHeaders(bool initialPopulation)
        {
            List<ChildBranchInfo> childBranches = new List<ChildBranchInfo>(1);

            IBranch assocSetBranch = null;
            if (initialPopulation)
            {
                assocSetBranch = new AssociationSetBranch(_mappingAssociation, _columns);
            }
            else
            {
                var locateData = LocateObject("ASSOCIATION", ObjectStyle.TrackingObject, 0);
                assocSetBranch = locateData.Row >= 0
                                     ? GetObject(locateData.Row, 0, ObjectStyle.ExpandedBranch) as IBranch
                                     : new AssociationSetBranch(_mappingAssociation, _columns);
            }
            childBranches.Add(new ChildBranchInfo(assocSetBranch, EdmxDesignerResources.MappingDetails_AssociationHeader, "ASSOCIATION"));

            SetHeaderInfo(childBranches.ToArray(), _columns);
        }
    }
}
