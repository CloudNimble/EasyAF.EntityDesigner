// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Data.Entity.Design.Model.Visitor
{
    internal interface IVisitable
    {
        IEnumerable<IVisitable> Accept(Visitor visitor);
    }
}
