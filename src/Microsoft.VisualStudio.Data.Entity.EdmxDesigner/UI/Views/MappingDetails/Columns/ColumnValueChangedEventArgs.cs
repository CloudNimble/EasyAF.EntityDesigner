// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell;
using System;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Views.MappingDetails.Columns
{

    internal class ColumnValueChangedEventArgs : EventArgs
    {
        internal TreeGridDesignerBranchChangedArgs Args { get; set; }

        internal ColumnValueChangedEventArgs(TreeGridDesignerBranchChangedArgs args)
        {
            Args = args;
        }

        internal static ColumnValueChangedEventArgs Default
        {
            get { return new ColumnValueChangedEventArgs(null); }
        }
    }

}
