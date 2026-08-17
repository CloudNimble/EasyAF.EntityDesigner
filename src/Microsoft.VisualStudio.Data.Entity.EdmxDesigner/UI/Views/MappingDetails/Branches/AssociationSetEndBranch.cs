// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Base.Shell;
using Microsoft.Data.Tools.VSXmlDesignerBase.VirtualTreeGrid;
using Microsoft.VisualStudio.Data.Entity.Design.UI.ViewModels.MappingDetails.Associations;

namespace Microsoft.VisualStudio.Data.Entity.Design.UI.Views.MappingDetails.Branches
{
    internal class AssociationSetEndBranch : TreeGridDesignerBranch
    {
        private MappingAssociationSet _mappingAssociationSet;

        internal AssociationSetEndBranch(MappingAssociationSet mappingAssociationSet, TreeGridDesignerColumnDescriptor[] columns)
            : base(mappingAssociationSet, columns)
        {
            _mappingAssociationSet = mappingAssociationSet;
        }

        public AssociationSetEndBranch()
        {
        }

        public override bool Initialize(object component, TreeGridDesignerColumnDescriptor[] columns)
        {
            if (!base.Initialize(component, columns))
            {
                return false;
            }

            if (component is MappingAssociationSet mappingAssociationSet)
            {
                _mappingAssociationSet = mappingAssociationSet;
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
            return _mappingAssociationSet.Children[index];
        }

        internal override object GetCreatorElement()
        {
            return null;
        }

        internal override int GetIndexForElement(object element)
        {
            for (var i = 0; i < _mappingAssociationSet.Children.Count; i++)
            {
                if (element == _mappingAssociationSet.Children[i])
                {
                    return i;
                }
            }

            return -1;
        }

        internal override int ElementCount
        {
            get { return _mappingAssociationSet.Children.Count; }
        }

        protected override bool IsExpandable(int index)
        {
            return (index < ElementCount);
        }

        protected override IBranch GetExpandedBranch(int index)
        {
            if (index < ElementCount)
            {
                if (GetElement(index) is MappingAssociationSetEnd mase)
                {
                    return new EndScalarPropertyBranch(mase, GetColumns());
                }
            }

            return null;
        }
    }
}
