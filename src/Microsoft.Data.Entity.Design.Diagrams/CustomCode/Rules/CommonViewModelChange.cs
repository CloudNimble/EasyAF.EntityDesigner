// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

namespace Microsoft.Data.Entity.Design.Diagrams.Rules
{

    internal abstract class CommonViewModelChange
    {
        /// <summary>
        ///     Changes will be invoked in order of priority (less number means it will be invoked sooner)
        ///     This property MUST be immutable since changes are sorted based on this
        /// </summary>
        internal virtual int InvokeOrderPriority
        {
            get { return 1000; }
        }
    }

}
