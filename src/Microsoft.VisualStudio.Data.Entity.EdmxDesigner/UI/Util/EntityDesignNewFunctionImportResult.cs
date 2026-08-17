// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Database;
using Microsoft.Data.Entity.Design.Edmx.Entity;
using System.Windows.Forms;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.Util
{
    internal struct EntityDesignNewFunctionImportResult
    {
        public DialogResult DialogResult { get; set; }
        public Function Function { get; set; }
        public string FunctionName { get; set; }
        public bool IsComposable { get; set; }
        public object ReturnType { get; set; }
        public IDataSchemaProcedure Schema { get; set; }
    }
}
