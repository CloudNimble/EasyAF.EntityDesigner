// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Dialog
{

    /// <summary>
    /// Data item for the return type columns ListView.
    /// </summary>
    internal class ReturnTypeColumnItem
    {
        public string Action { get; set; }
        public string Name { get; set; }
        public string EdmType { get; set; }
        public string DbType { get; set; }
        public string Nullable { get; set; }
        public string MaxLength { get; set; }
        public string Precision { get; set; }
        public string Scale { get; set; }
    }

}
