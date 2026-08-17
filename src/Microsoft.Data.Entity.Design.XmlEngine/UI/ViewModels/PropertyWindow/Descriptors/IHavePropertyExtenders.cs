// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.UI.ViewModels.PropertyWindow.Descriptors
{
    /// <summary>
    ///     this interface is to be implemented by descriptors and expandable property
    ///     objects that have property extenders. Property extenders allow for sharing
    ///     the implementation of properties that are common to different objects.
    /// </summary>
    internal interface IHavePropertyExtenders
    {
        IList<object> GetPropertyExtenders();
    }
}
