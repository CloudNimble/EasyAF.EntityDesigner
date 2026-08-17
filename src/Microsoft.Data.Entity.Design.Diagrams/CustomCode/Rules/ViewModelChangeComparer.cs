// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    internal class ViewModelChangeComparer : IComparer<CommonViewModelChange>
    {
        public int Compare(CommonViewModelChange x, CommonViewModelChange y)
        {
            return x.InvokeOrderPriority < y.InvokeOrderPriority ? -1 : 1;
        }
    }

}
