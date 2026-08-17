// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.Tables;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Branches
{
    // <summary>
    //     This is the header branch for the selected Entity Type Mapping.  It displays the
    //     tables that are mapped to this entity.
    // </summary>
    internal class EntityTypeBranch : HeaderBranch
    {
        private MappingConceptualEntityType _mappingConceptualTypeMapping;
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

            _mappingConceptualTypeMapping = component as MappingConceptualEntityType;
            if (_mappingConceptualTypeMapping != null)
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

            IBranch tableBranch = null;
            if (initialPopulation)
            {
                tableBranch = new TableBranch(_mappingConceptualTypeMapping, _columns);
            }
            else
            {
                var locateData = LocateObject("TABLE", ObjectStyle.TrackingObject, 0);
                tableBranch = locateData.Row >= 0
                                  ? GetObject(locateData.Row, 0, ObjectStyle.ExpandedBranch) as IBranch
                                  : new TableBranch(_mappingConceptualTypeMapping, _columns);
            }
            childBranches.Add(new ChildBranchInfo(tableBranch, Resources.MappingDetails_TablesHeader, "TABLE"));

            SetHeaderInfo(childBranches.ToArray(), _columns);
        }
    }
}
