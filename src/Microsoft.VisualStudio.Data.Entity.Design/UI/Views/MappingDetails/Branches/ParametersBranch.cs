// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Shell;
using Microsoft.Data.Entity.Design.UI.ViewModels.MappingDetails.Functions;
using Microsoft.Data.Tools.VSXmlDesignerBase.VirtualTreeGrid;

namespace Microsoft.Data.Entity.Design.UI.Views.MappingDetails.Branches
{
    // <summary>
    //     The purpose of this class is to create the container node for the parameters.  So,
    //     there is only ever one item, one row.
    // </summary>
    internal class ParametersBranch : TreeGridDesignerBranch
    {
        private MappingModificationFunctionMapping _mappingModificationFunctionMapping;

        internal ParametersBranch(
            MappingModificationFunctionMapping mappingModificationFunctionMapping, TreeGridDesignerColumnDescriptor[] columns)
            : base(mappingModificationFunctionMapping, columns)
        {
            _mappingModificationFunctionMapping = mappingModificationFunctionMapping;
        }

        public ParametersBranch()
        {
        }

        public override bool Initialize(object component, TreeGridDesignerColumnDescriptor[] columns)
        {
            if (!base.Initialize(component, columns))
            {
                return false;
            }

            if (component is MappingModificationFunctionMapping mappingModificationFunctionMapping)
            {
                _mappingModificationFunctionMapping = mappingModificationFunctionMapping;
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
                // this branch just displays text in the first column
                return string.Empty;
            }
        }

        internal override object GetElement(int index)
        {
            return _mappingModificationFunctionMapping.MappingFunctionScalarProperties;
        }

        internal override object GetCreatorElement()
        {
            return null;
        }

        internal override int GetIndexForElement(object element)
        {
            return 0;
        }

        internal override int ElementCount
        {
            get { return _mappingModificationFunctionMapping.MappingFunctionScalarProperties == null ? 0 : 1; }
        }

        protected override bool IsExpandable(int index)
        {
            return (index < ElementCount);
        }

        protected override IBranch GetExpandedBranch(int index)
        {
            if (index < ElementCount)
            {
                if (GetElement(index) is MappingFunctionScalarProperties msp)
                {
                    return new ParameterBranch(msp, GetColumns());
                }
            }

            return null;
        }

        protected override VirtualTreeDisplayData GetDisplayData(int row, int column, VirtualTreeDisplayDataMasks requiredData)
        {
            var data = base.GetDisplayData(row, column, requiredData);
            if (column == 0)
            {
                data.Image = data.SelectedImage = MappingDetailsImages.ICONS_FOLDER;
                data.ImageList = MappingDetailsImages.GetIconsImageList();
            }

            return data;
        }
    }
}
