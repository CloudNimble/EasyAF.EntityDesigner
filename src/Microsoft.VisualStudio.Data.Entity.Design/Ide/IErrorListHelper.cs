// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.Data.Entity.Design.Ide
{
    internal interface IErrorListHelper
    {
        void AddErrorInfosToErrorList(
            ICollection<ErrorInfo> errors, IVsHierarchy vsHierarchy, uint itemID, bool bringErrorListToFront = false);
    }
}
