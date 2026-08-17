// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.Data.Entity.Design.Edmx.Entity
{

    /// <summary>
    ///     Helper class that store the information where a property should be inserted.
    /// </summary>
    internal class InsertPropertyPosition
    {
        internal PropertyBase InsertAtProperty;
        internal bool InsertBefore; // Flag whether to insert before or insert after InsertAt.

        internal InsertPropertyPosition(PropertyBase insertAtProperty, bool insertBefore)
        {
            Debug.Assert(insertAtProperty != null, "Parameter insertAt cannot be null.");
            InsertBefore = insertBefore;
            InsertAtProperty = insertAtProperty;
        }
    }

}
