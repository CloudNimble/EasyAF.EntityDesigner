// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Data.Entity.XmlDesigner.UI.ViewModels.Explorer;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.UI.ViewModels.Explorer
{

    // <summary>
    //     Extension methods for the IExplorerViewModel interface.
    // </summary>
    internal static class Explorer_IExplorerViewModelExtensions
    {
        internal static ExplorerRootNode EDMRootNode(this IExplorerViewModel viewModel)
        {
            return ((ExplorerViewModel)viewModel).EDMRootNode;
        }
    }

}
