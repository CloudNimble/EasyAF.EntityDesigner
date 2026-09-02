// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer
{

    /// <summary>
    ///     Represents the ViewModel that will be exposed in the Explorer Window.
    /// </summary>
    internal interface IExplorerViewModel
    {
        EditingContext EditingContext { get; }
        ExplorerEFElement RootNode { get; }
    }

}
