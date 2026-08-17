// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Model
{
    internal abstract class ModelChangeEventArgs : EventArgs
    {
        public abstract IEnumerable<ModelNodeChangeInfo> Changes { get; }
    }
}
