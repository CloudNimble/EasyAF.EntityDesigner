// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Shell;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Associations;
using Microsoft.Data.Tools.VSXmlDesignerBase.VirtualTreeGrid;

namespace Microsoft.Data.Entity.Design.UI.Views.MappingDetails.Branches
{
    internal class AssociationSetBranch : TreeGridDesignerBranch
    {
        private MappingAssociation _mappingAssociation;
        private IBranch _expandedBranch;
        //private bool _registeredEventHandlers = false;

        internal AssociationSetBranch(MappingAssociation mappingAssociation, TreeGridDesignerColumnDescriptor[] columns)
            : base(mappingAssociation, columns)
        {
            _mappingAssociation = mappingAssociation;
        }

        public AssociationSetBranch()
        {
        }

        public override bool Initialize(object component, TreeGridDesignerColumnDescriptor[] columns)
        {
            if (!base.Initialize(component, columns))
            {
                return false;
            }

            if (component is MappingAssociation mappingAssociation)
            {
                _mappingAssociation = mappingAssociation;
            }

            return true;
        }

        protected override string GetText(int row, int column)
        {
            if (column == 0)
            {
                return base.GetText(row, column);
            }
            else
            {
                return string.Empty;
            }
        }

        internal override object GetElement(int index)
        {
            return _mappingAssociation.Children[index];
        }

        internal override object GetCreatorElement()
        {
            return null;
        }

        internal override int GetIndexForElement(object element)
        {
            for (var i = 0; i < _mappingAssociation.Children.Count; i++)
            {
                if (element == _mappingAssociation.Children[i])
                {
                    return i;
                }
            }

            return -1;
        }

        internal override int ElementCount
        {
            get { return _mappingAssociation.Children.Count; }
        }

        internal override int CreatorNodeCount
        {
            get { return 0; }
        }

        protected override bool IsExpandable(int index)
        {
            if (index < ElementCount)
            {
                var a = _mappingAssociation.Association;
                if (a != null)
                {
                    var aSet = a.AssociationSet;
                    if (aSet != null
                        && aSet.AssociationSetMapping != null)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override IBranch GetExpandedBranch(int index)
        {
            if (index < ElementCount)
            {
                if (GetElement(index) is MappingAssociationSet mas)
                {
                    _expandedBranch = new AssociationSetEndBranch(mas, GetColumns());
                    return _expandedBranch;
                }
            }

            return null;
        }

        protected override VirtualTreeDisplayData GetDisplayData(int row, int column, VirtualTreeDisplayDataMasks requiredData)
        {
            var data = base.GetDisplayData(row, column, requiredData);
            if (column == 0)
            {
                data.Image = data.SelectedImage = MappingDetailsImages.ICONS_TABLE;
                data.ImageList = MappingDetailsImages.GetIconsImageList();
            }

            return data;
        }

        public override void OnColumnValueChanged(TreeGridDesignerBranchChangedArgs args)
        {
            if (!args.InsertingItem
                && _expandedBranch != null)
            {
                DoBranchModification(BranchModificationEventArgs.RemoveBranch(_expandedBranch));
                _expandedBranch = null;
            }
        }
    }
}
