// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.Data.Entity.Design.VisualStudio
{
    internal interface IErrorListHelper
    {
        void AddErrorInfosToErrorList(
            ICollection<ErrorInfo> errors, IVsHierarchy vsHierarchy, uint itemID, bool bringErrorListToFront = false);
    }
}
