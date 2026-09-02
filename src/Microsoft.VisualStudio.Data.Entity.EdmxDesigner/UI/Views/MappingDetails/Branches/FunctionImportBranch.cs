// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.MappingDetails.FunctionImports;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using Microsoft.VisualStudio.Data.Entity.XmlDesigner.VirtualTreeGrid.Provider;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Branches
{
    internal class FunctionImportBranch : TreeGridDesignerBranch
    {
        private MappingFunctionImport _mappingFunctionImport;

        public override bool Initialize(object component, TreeGridDesignerColumnDescriptor[] columns)
        {
            if (!base.Initialize(component, columns))
            {
                return false;
            }

            _mappingFunctionImport = component as MappingFunctionImport;
            if (_mappingFunctionImport != null)
            {
                return true;
            }

            return false;
        }

        internal override int ElementCount
        {
            get { return 1; }
        }

        internal override object GetCreatorElement()
        {
            return null;
        }

        internal override object GetElement(int index)
        {
            return _mappingFunctionImport;
        }

        internal override int GetIndexForElement(object element)
        {
            return 0;
        }

        protected override bool IsExpandable(int index)
        {
            return (index < ElementCount);
        }

        protected override IBranch GetExpandedBranch(int index)
        {
            if (index == 0)
            {
                return new FunctionImportScalarPropertyBranch(_mappingFunctionImport, GetColumns());
            }

            return null;
        }
    }
}
