// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.XmlEngine.Context;
using System;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Base.Shell
{
    /// <summary>
    ///     Provides a mechanism for columns to call back on the hosting grid
    /// </summary>
    internal interface ITreeGridDesignerColumnHost : IServiceProvider
    {
        /// <summary>
        ///     Invalidates the grid
        /// </summary>
        /// <param name="tracking">
        ///     If specified, only the row corresponding to this tracking object will be invalidated.
        ///     If null, the entire grid will be invalidated.
        /// </param>
        void Invalidate(object tracking);

        /// <summary>
        ///     Retrieves the current label edit state of the tree control.
        /// </summary>
        bool InLabelEdit { get; }

        /// <summary>
        ///     Retrieves the current editing context
        /// </summary>
        EditingContext Context { get; set; }

        /// <summary>
        ///     Expands all of the nodes in the tree
        /// </summary>
        void ExpandAll();

        /// <summary>
        ///     Reloads the root of the tree
        /// </summary>
        void ReloadRoot();
    }
}
