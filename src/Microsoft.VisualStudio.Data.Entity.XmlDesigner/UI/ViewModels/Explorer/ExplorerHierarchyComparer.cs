// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer
{

    internal class ExplorerHierarchyComparer : IComparer<ExplorerEFElement>
    {
        private static ExplorerHierarchyComparer _instance;

        internal static ExplorerHierarchyComparer Instance
        {
            get
            {
                if (null == _instance)
                {
                    _instance = new ExplorerHierarchyComparer();
                }

                return _instance;
            }
        }

        private ExplorerHierarchyComparer()
        {
            // constructor made private to implement singleton pattern
        }

        public int Compare(ExplorerEFElement x, ExplorerEFElement y)
        {
            return ExplorerEFElement.HierarchyCompare(x, y);
        }
    }

}
