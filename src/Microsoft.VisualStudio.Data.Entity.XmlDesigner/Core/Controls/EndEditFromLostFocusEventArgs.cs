// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Windows;

namespace Microsoft.VisualStudio.Data.Entity.XmlDesigner.Core.Controls
{

    /// <summary>
    ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public class EndEditFromLostFocusEventArgs : EventArgs
    {
        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        /// <param name="newFocusElement">This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.</param>
        public EndEditFromLostFocusEventArgs(IInputElement newFocusElement)
        {
            NewFocusElement = newFocusElement;
        }

        /// <summary>
        ///     This API supports the Entity Framework infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public IInputElement NewFocusElement { get; private set; }
    }

}
